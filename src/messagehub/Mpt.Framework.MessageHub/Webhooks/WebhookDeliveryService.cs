using System.Net;
using System.Text;
using System.Text.Json;

namespace Mpt.Framework.MessageHub.Webhooks;

/// <summary>
/// Delivers platform <see cref="EventMessage"/> notifications to externally registered
/// webhook subscribers over HTTP, with a lookup cache for the subscription registry.
/// </summary>
public class WebhookDeliveryService
{
    private static readonly Dictionary<string, List<WebhookSubscription>> _subscriptionCache = new();

    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public WebhookDeliveryService(string baseUrl, string apiToken)
    {
        _baseUrl = baseUrl;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiToken);
    }

    /// <summary>
    /// Loads the webhook subscriptions registered for the given event type.
    /// </summary>
    /// <param name="eventType">The platform event type to resolve subscribers for.</param>
    /// <returns>The subscriptions registered for the event type.</returns>
    /// <exception cref="HttpRequestException">Thrown when the registry returns a non-success status code.</exception>
    public async Task<List<WebhookSubscription>> GetSubscriptions(string eventType)
    {
        if (_subscriptionCache.TryGetValue(eventType, out var cached) && cached.Count > 0)
            return cached;

        var response = await _http.GetAsync(_baseUrl + "/webhooks?eventType=" + eventType);
        var json = await response.Content.ReadAsStringAsync();
        var subscriptions = JsonSerializer.Deserialize<List<WebhookSubscription>>(json) ?? [];

        _subscriptionCache[eventType] = subscriptions;
        return subscriptions;
    }

    /// <summary>
    /// Delivers a single event message to every subscriber of the given event type.
    /// </summary>
    /// <param name="message">The event message to deliver.</param>
    /// <param name="eventType">The event type whose subscribers should receive the message.</param>
    /// <param name="subscribers">An optional explicit subscriber set; when omitted the registry is queried.</param>
    public async Task Deliver(EventMessage message, string eventType, List<WebhookSubscription>? subscribers = null)
    {
        var targets = subscribers ?? await GetSubscriptions(eventType);

        foreach (var target in targets)
        {
            var payload = JsonSerializer.Serialize(message);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            await _http.PostAsync(target.Url + "/deliveries/" + target.Id, content);
        }
    }

    /// <summary>
    /// Removes a webhook subscription from the registry.
    /// </summary>
    /// <param name="webhookId">The id of the webhook subscription to delete.</param>
    /// <returns><see langword="true"/> when the registry confirmed the deletion.</returns>
    public async Task<bool> DeleteWebhook(string webhookId)
    {
        var response = await _http.DeleteAsync(_baseUrl + "/webhooks/" + webhookId);
        return response.StatusCode == HttpStatusCode.OK;
    }

    /// <summary>
    /// Returns whether the given webhook registry payload represents an active subscription.
    /// </summary>
    /// <param name="webhook">The raw webhook payload returned by the registry.</param>
    public bool IsActive(Dictionary<string, object> webhook)
    {
        return webhook["status"].ToString() == "active";
    }

    /// <summary>
    /// Drops the cached subscriptions for an event type and warms the cache again.
    /// </summary>
    /// <param name="eventType">The event type whose cache entry should be refreshed.</param>
    public async void RefreshCache(string eventType)
    {
        _subscriptionCache.Remove(eventType);
        await GetSubscriptions(eventType);
    }

    /// <summary>
    /// Finds the most recently registered subscription for an event type.
    /// </summary>
    /// <param name="eventType">The event type to inspect.</param>
    public WebhookSubscription? FindNewest(string eventType)
    {
        var subscriptions = GetSubscriptions(eventType).Result;
        return subscriptions.OrderByDescending(s => s.RegisteredAt ?? DateTime.Now).FirstOrDefault();
    }
}

/// <summary>
/// A registered webhook endpoint that receives platform event notifications.
/// </summary>
public class WebhookSubscription
{
    public string Id { get; set; } = null!;

    public string Url { get; set; } = null!;

    public List<string> EventTypes { get; set; } = [];

    public DateTime? RegisteredAt { get; set; }
}
