namespace LatticeBoltzmann.Core.Tests;

public class D2Q9Tests
{
    private static readonly int[] ExpectedEx = [0, 1, 0, -1, 0, 1, -1, -1, 1];
    private static readonly int[] ExpectedEy = [0, 0, 1, 0, -1, 1, 1, -1, -1];
    private static readonly int[] ExpectedOpposite = [0, 3, 4, 1, 2, 7, 8, 5, 6];

    [Fact]
    public void Directions_follow_spec_order()
    {
        Assert.Equal(ExpectedEx, D2Q9.Ex.ToArray());
        Assert.Equal(ExpectedEy, D2Q9.Ey.ToArray());
        Assert.Equal(ExpectedOpposite, D2Q9.Opposite.ToArray());
        Assert.Equal(9, D2Q9.Q);
    }

    [Fact]
    public void Weights_sum_to_one()
    {
        double sum = 0;
        foreach (var w in D2Q9.Weights) sum += w;
        Assert.Equal(1.0, sum, 15);
    }

    [Fact]
    public void Weighted_moments_match_isotropic_lattice()
    {
        double mx = 0, my = 0, mxx = 0, myy = 0, mxy = 0;
        for (var i = 0; i < D2Q9.Q; i++)
        {
            var w = D2Q9.Weights[i];
            mx += w * D2Q9.Ex[i]; my += w * D2Q9.Ey[i];
            mxx += w * D2Q9.Ex[i] * D2Q9.Ex[i];
            myy += w * D2Q9.Ey[i] * D2Q9.Ey[i];
            mxy += w * D2Q9.Ex[i] * D2Q9.Ey[i];
        }
        Assert.Equal(0, mx, 15); Assert.Equal(0, my, 15); Assert.Equal(0, mxy, 15);
        Assert.Equal(D2Q9.SoundSpeedSquared, mxx, 15);
        Assert.Equal(D2Q9.SoundSpeedSquared, myy, 15);
    }

    [Fact]
    public void Opposite_reverses_each_direction()
    {
        for (var i = 0; i < D2Q9.Q; i++)
        {
            var o = D2Q9.Opposite[i];
            Assert.Equal(i, D2Q9.Opposite[o]);
            Assert.Equal(-D2Q9.Ex[i], D2Q9.Ex[o]);
            Assert.Equal(-D2Q9.Ey[i], D2Q9.Ey[o]);
        }
    }
}
