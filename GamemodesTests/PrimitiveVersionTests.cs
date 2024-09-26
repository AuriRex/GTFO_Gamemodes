using Gamemodes;
using System;

namespace GamemodesTests;

public class PrimitiveVersionTests
{
    public const string VERSION_ONE = "1.0.0";
    public const string VERSION_ONE_PATCH = "1.0.1";
    public const string VERSION_ONE_MINOR = "1.1.0";
    public const string VERSION_ONE_MAJOR = "2.0.0";

    private PrimitiveVersion baseVersion;
    private PrimitiveVersion biggerPatch;
    private PrimitiveVersion biggerMinor;
    private PrimitiveVersion biggerMajor;

    [SetUp]
    public void Setup()
    {
        baseVersion = new PrimitiveVersion(VERSION_ONE);
        biggerPatch = new PrimitiveVersion(VERSION_ONE_PATCH);
        biggerMinor = new PrimitiveVersion(VERSION_ONE_MINOR);
        biggerMajor = new PrimitiveVersion(VERSION_ONE_MAJOR);
    }

    [Test]
    public void Equals()
    {
        Assert.That(new PrimitiveVersion(VERSION_ONE), Is.EqualTo(baseVersion));
        Assert.That(baseVersion.ToString(), Is.EqualTo(VERSION_ONE));
    }

    [Test]
    public void Greater()
    {
        Assert.Multiple(() =>
        {
            Assert.That(biggerPatch > baseVersion);
            Assert.That(biggerMinor > baseVersion);
            Assert.That(biggerMajor > baseVersion);

            Assert.That(biggerMinor > biggerPatch);
            Assert.That(biggerMajor > biggerPatch);

            Assert.That(biggerMajor > biggerMinor);
        });
    }

    [Test]
    public void Smaller()
    {

        Assert.Multiple(() =>
        {
            Assert.That(!(biggerPatch < baseVersion));
            Assert.That(!(biggerMinor < baseVersion));
            Assert.That(!(biggerMajor < baseVersion));

            Assert.That(!(biggerMinor < biggerPatch));
            Assert.That(!(biggerMajor < biggerPatch));

            Assert.That(!(biggerMajor < biggerMinor));
        });
    }

    [Test]
    public void SingleDigit()
    {
        Assert.That(new PrimitiveVersion($"1").ToString(), Is.EqualTo(VERSION_ONE));
    }

    [Test]
    public void TwoDigits()
    {
        Assert.That(new PrimitiveVersion($"1.0").ToString(), Is.EqualTo(VERSION_ONE));
    }

    [Test]
    public void FourDigits()
    {
        Assert.That(new PrimitiveVersion($"{VERSION_ONE}.0").ToString(), Is.EqualTo(VERSION_ONE));
    }

    [Test]
    public void Throws()
    {
        Assert.Throws<ArgumentException>(() => new PrimitiveVersion("garbage.data"));
    }
}