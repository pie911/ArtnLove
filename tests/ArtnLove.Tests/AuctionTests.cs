using System;
using ArtnLove.Services;
using Xunit;

namespace ArtnLove.Tests;

public class AuctionTests
{
    [Fact]
    public void PlaceBid_AcceptsValidBid()
    {
        var manager = new AuctionManager();
        var artwork = Guid.NewGuid();
        var owner = Guid.NewGuid();

        var auction = manager.CreateAuction(artwork, owner, 10m, 1m, DateTimeOffset.UtcNow.AddMinutes(30));

        var bidder = Guid.NewGuid();
        var result = manager.PlaceBid(auction.Id, bidder, 10m);

        Assert.Equal(BidResult.Accepted, result);
        Assert.True(auction.Bids.Count == 1);
    }

    [Fact]
    public void PlaceBid_RejectsTooLow()
    {
        var manager = new AuctionManager();
        var artwork = Guid.NewGuid();
        var owner = Guid.NewGuid();

        var auction = manager.CreateAuction(artwork, owner, 100m, 5m, DateTimeOffset.UtcNow.AddMinutes(30));

        var bidder = Guid.NewGuid();
        var result1 = manager.PlaceBid(auction.Id, bidder, 100m);
        Assert.Equal(BidResult.Accepted, result1);

        var result2 = manager.PlaceBid(auction.Id, Guid.NewGuid(), 102m); // less than min increment
        Assert.Equal(BidResult.TooLow, result2);
    }
}
