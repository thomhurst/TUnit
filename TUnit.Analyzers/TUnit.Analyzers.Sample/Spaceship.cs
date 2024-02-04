using System;

namespace TUnit.Analyzers.Sample;

public class Spaceship
{
    public void SetSpeed(long speed)
    {
        if (speed > 299_792_458)
            throw new ArgumentOutOfRangeException(nameof(speed));
    }
}