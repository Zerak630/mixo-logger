using Domain.Cocktails;

namespace Domain.Units;

public static class VolumeConverter
{
    public static Volume Convert(Volume volume, UniteVolume toUnit) => 
        new(Convert(volume.Value, volume.Unit, toUnit), toUnit);
    public static double Convert(double value, UniteVolume fromUnit, UniteVolume toUnit)
    {
        if (fromUnit == toUnit)
            return value;

        double valueInMl = fromUnit.ToString() switch
        {
            UniteVolume.mL => value,
            UniteVolume.cL => value * 10,
            UniteVolume.dL => value * 100,
            UniteVolume.L => value * 1000,
            _ => throw new ArgumentException("Unsupported unit", nameof(fromUnit))
        };

        return toUnit.ToString() switch
        {
            UniteVolume.mL => valueInMl,
            UniteVolume.cL => valueInMl / 10,
            UniteVolume.dL => valueInMl / 100,
            UniteVolume.L => valueInMl / 1000,
            _ => throw new ArgumentException("Unsupported unit", nameof(toUnit))
        };
    }
}