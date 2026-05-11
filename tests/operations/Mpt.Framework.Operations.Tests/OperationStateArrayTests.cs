using FluentAssertions;
using Mpt.Framework.Operations;

namespace Mpt.Framework.Operations.Tests;

public class OperationStateArrayTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var data = new byte[2]; // Can hold 8 items (2 bytes * 4 items per byte)

        // Act
        var array = new OperationStateArray(data, 8);

        // Assert
        array.Length.Should().Be(8); // 2 bytes * 4 items per byte
        array.Data.Should().BeSameAs(data);
    }

    [Fact]
    public void Constructor_WithLength_ShouldCreateCorrectSizedArray()
    {
        // Arrange
        var length = 10;

        // Act
        var array = new OperationStateArray(length);

        // Assert
        array.Length.Should().Be(length);
        array.Data.Length.Should().Be((length + 3) / 4); // 3 bytes for 10 items
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Constructor_WithInvalidLength_ShouldThrowArgumentException(int invalidLength)
    {
        // Act & Assert
        Action act = () => new OperationStateArray(invalidLength);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("Length must be positive. (Parameter 'length')");
    }

    [Fact]
    public void Constructor_WithNullData_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new OperationStateArray(null!, 2);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(ItemState.Pending)]
    [InlineData(ItemState.Succeeded)]
    [InlineData(ItemState.Failed)]
    public void Set_WithValidIndexAndState_ShouldSetCorrectly(ItemState state)
    {
        // Arrange
        var array = new OperationStateArray(5);
        var index = 2;

        // Act
        array.Set(index, state);

        // Assert
        array.Get(index).Should().Be(state);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)] // Length is 5, so valid indices are 0-4
    [InlineData(10)]
    public void Set_WithInvalidIndex_ShouldThrowIndexOutOfRangeException(int invalidIndex)
    {
        // Arrange
        var array = new OperationStateArray(5);

        // Act & Assert
        Action act = () => array.Set(invalidIndex, ItemState.Pending);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)] // Length is 5, so valid indices are 0-4
    [InlineData(10)]
    public void Get_WithInvalidIndex_ShouldThrowIndexOutOfRangeException(int invalidIndex)
    {
        // Arrange
        var array = new OperationStateArray(5);

        // Act & Assert
        Action act = () => array.Get(invalidIndex);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Get_FromUninitialized_ShouldReturnPending()
    {
        // Arrange
        var array = new OperationStateArray(5);

        // Act & Assert
        for (int i = 0; i < array.Length; i++)
        {
            array.Get(i).Should().Be(ItemState.Pending);
        }
    }

    [Fact]
    public void SetAndGet_MultipleStatesInSameByte_ShouldNotInterfere()
    {
        // Arrange
        var data = new byte[1]; // 1 byte holds 4 items
        var array = new OperationStateArray(data, 4);

        // Act
        array.Set(0, ItemState.Pending);
        array.Set(1, ItemState.Succeeded);
        array.Set(2, ItemState.Failed);
        array.Set(3, ItemState.Succeeded);

        // Assert
        array.Get(0).Should().Be(ItemState.Pending);
        array.Get(1).Should().Be(ItemState.Succeeded);
        array.Get(2).Should().Be(ItemState.Failed);
        array.Get(3).Should().Be(ItemState.Succeeded);
    }

    [Fact]
    public void SetAndGet_MultipleStatesAcrossBytes_ShouldWorkCorrectly()
    {
        // Arrange
        var data = new byte[3]; // 3 bytes hold 12 items
        var array = new OperationStateArray(data, 12);

        // Act
        array.Set(0, ItemState.Pending);
        array.Set(3, ItemState.Succeeded);  // Last item in first byte
        array.Set(4, ItemState.Failed);     // First item in second byte
        array.Set(7, ItemState.Succeeded);  // Last item in second byte
        array.Set(8, ItemState.Failed);     // First item in third byte
        array.Set(9, ItemState.Pending);    // Second item in third byte

        // Assert
        array.Get(0).Should().Be(ItemState.Pending);
        array.Get(3).Should().Be(ItemState.Succeeded);
        array.Get(4).Should().Be(ItemState.Failed);
        array.Get(7).Should().Be(ItemState.Succeeded);
        array.Get(8).Should().Be(ItemState.Failed);
        array.Get(9).Should().Be(ItemState.Pending);
    }

    [Fact]
    public void Set_OverwriteExistingState_ShouldUpdateCorrectly()
    {
        // Arrange
        var array = new OperationStateArray(5);
        var index = 2;

        // Act
        array.Set(index, ItemState.Pending);
        array.Get(index).Should().Be(ItemState.Pending);

        array.Set(index, ItemState.Succeeded);
        array.Get(index).Should().Be(ItemState.Succeeded);

        array.Set(index, ItemState.Failed);
        array.Get(index).Should().Be(ItemState.Failed);

        array.Set(index, ItemState.Pending);

        // Assert
        array.Get(index).Should().Be(ItemState.Pending);
    }

    [Fact]
    public void GetCounters_EmptyArray_ShouldReturnAllPending()
    {
        // Arrange
        var array = new OperationStateArray(5);

        // Act
        var counters = array.GetCounters();

        // Assert
        counters[ItemState.Pending].Should().Be(5);
        counters[ItemState.Succeeded].Should().Be(0);
        counters[ItemState.Failed].Should().Be(0);
    }

    [Fact]
    public void GetCounters_MixedStates_ShouldCountCorrectly()
    {
        // Arrange
        var array = new OperationStateArray(10);

        // Set various states
        array.Set(0, ItemState.Pending);    // 1 pending
        array.Set(1, ItemState.Pending);    // 2 pending
        array.Set(2, ItemState.Succeeded);  // 1 succeeded
        array.Set(3, ItemState.Succeeded);  // 2 succeeded
        array.Set(4, ItemState.Succeeded);  // 3 succeeded
        array.Set(5, ItemState.Failed);     // 1 failed
        array.Set(6, ItemState.Failed);     // 2 failed
        // Indices 7, 8, 9 remain pending   // 5 pending total

        // Act
        var counters = array.GetCounters();

        // Assert
        counters[ItemState.Pending].Should().Be(5);
        counters[ItemState.Succeeded].Should().Be(3);
        counters[ItemState.Failed].Should().Be(2);
    }

    [Fact]
    public void GetCounters_SingleByte_ShouldCountCorrectly()
    {
        // Arrange
        var array = new OperationStateArray(4); // Fits in single byte

        array.Set(0, ItemState.Pending);
        array.Set(1, ItemState.Succeeded);
        array.Set(2, ItemState.Failed);
        array.Set(3, ItemState.Succeeded);

        // Act
        var counters = array.GetCounters();

        // Assert
        counters[ItemState.Pending].Should().Be(1);
        counters[ItemState.Succeeded].Should().Be(2);
        counters[ItemState.Failed].Should().Be(1);
    }

    [Fact]
    public void GetCounters_PartialLastByte_ShouldCountCorrectly()
    {
        // Arrange
        var array = new OperationStateArray(6); // 1.5 bytes, so last byte is partial

        array.Set(0, ItemState.Pending);
        array.Set(1, ItemState.Succeeded);
        array.Set(2, ItemState.Failed);
        array.Set(3, ItemState.Succeeded);
        array.Set(4, ItemState.Failed);
        array.Set(5, ItemState.Pending);

        // Act
        var counters = array.GetCounters();

        // Assert
        counters[ItemState.Pending].Should().Be(2);
        counters[ItemState.Succeeded].Should().Be(2);
        counters[ItemState.Failed].Should().Be(2);
    }

    [Fact]
    public void GetCounters_LargeArray_ShouldCountCorrectly()
    {
        // Arrange
        var array = new OperationStateArray(1000);

        // Set every 3rd item to Succeeded, every 5th to Failed, rest remain Pending
        for (int i = 0; i < array.Length; i++)
        {
            if (i % 3 == 0) array.Set(i, ItemState.Succeeded);
            else if (i % 5 == 0) array.Set(i, ItemState.Failed);
            // else remains Pending (default)
        }

        // Act
        var counters = array.GetCounters();

        // Assert
        var expectedSucceeded = 0;
        var expectedFailed = 0;
        for (int i = 0; i < 1000; i++)
        {
            if (i % 3 == 0) expectedSucceeded++;
            else if (i % 5 == 0) expectedFailed++;
        }
        var expectedPending = 1000 - expectedSucceeded - expectedFailed;

        counters[ItemState.Pending].Should().Be(expectedPending);
        counters[ItemState.Succeeded].Should().Be(expectedSucceeded);
        counters[ItemState.Failed].Should().Be(expectedFailed);
    }

    [Fact]
    public void Data_ShouldReturnUnderlyingByteArray()
    {
        // Arrange
        var originalData = new byte[] { 0x12, 0x34 };
        var array = new OperationStateArray(originalData, 8);

        // Act
        var data = array.Data;

        // Assert
        data.Should().BeSameAs(originalData);
    }

    [Fact]
    public void BitPacking_ShouldUseCorrectBitPositions()
    {
        // Arrange
        var array = new OperationStateArray(8); // 8 items span 2 bytes

        // Act - Set specific pattern to verify bit positions across 2 bytes
        // First byte (indices 0-3)
        array.Set(0, ItemState.Succeeded); // bits 0-1: 01
        array.Set(1, ItemState.Failed);    // bits 2-3: 10
        array.Set(2, ItemState.Pending);   // bits 4-5: 00
        array.Set(3, ItemState.Succeeded); // bits 6-7: 01

        // Second byte (indices 4-7)
        array.Set(4, ItemState.Failed);    // bits 0-1: 10
        array.Set(5, ItemState.Pending);   // bits 2-3: 00
        array.Set(6, ItemState.Succeeded); // bits 4-5: 01
        array.Set(7, ItemState.Failed);    // bits 6-7: 10

        // Verify individual values first
        array.Get(0).Should().Be(ItemState.Succeeded);
        array.Get(1).Should().Be(ItemState.Failed);
        array.Get(2).Should().Be(ItemState.Pending);
        array.Get(3).Should().Be(ItemState.Succeeded);
        array.Get(4).Should().Be(ItemState.Failed);
        array.Get(5).Should().Be(ItemState.Pending);
        array.Get(6).Should().Be(ItemState.Succeeded);
        array.Get(7).Should().Be(ItemState.Failed);

        // Verify byte patterns
        // First byte: 01 00 10 01 (from high to low bits) = 0x49
        array.Data[0].Should().Be(0x49);

        // Second byte: 10 01 00 10 (from high to low bits) = 0x92
        array.Data[1].Should().Be(0x92);
    }

    [Theory]
    [InlineData(1, 1)]   // 1 item needs 1 byte
    [InlineData(4, 1)]   // 4 items need 1 byte
    [InlineData(5, 2)]   // 5 items need 2 bytes
    [InlineData(8, 2)]   // 8 items need 2 bytes
    [InlineData(9, 3)]   // 9 items need 3 bytes
    [InlineData(12, 3)]  // 12 items need 3 bytes
    [InlineData(13, 4)]  // 13 items need 4 bytes
    public void Constructor_ShouldAllocateCorrectNumberOfBytes(int length, int expectedBytes)
    {
        // Act
        var array = new OperationStateArray(length);

        // Assert
        array.Data.Length.Should().Be(expectedBytes);
    }

    [Fact]
    public void StressTest_SetAndGetAllPositions_ShouldMaintainConsistency()
    {
        // Arrange
        var array = new OperationStateArray(100);
        var states = new ItemState[] { ItemState.Pending, ItemState.Succeeded, ItemState.Failed };
        var expectedStates = new ItemState[100];

        // Act - Set all positions to specific states
        for (int i = 0; i < array.Length; i++)
        {
            var state = states[i % 3];
            expectedStates[i] = state;
            array.Set(i, state);
        }

        // Assert - Verify all positions
        for (int i = 0; i < array.Length; i++)
        {
            array.Get(i).Should().Be(expectedStates[i], $"because index {i} should have state {expectedStates[i]}");
        }
    }
}
