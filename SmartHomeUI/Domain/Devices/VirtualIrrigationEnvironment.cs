using System;

namespace SmartHomeUI.Domain.Devices;

/// <summary>
/// Simulated environment for irrigation: soil moisture, ambient temperature, and rain intensity.
/// </summary>
public sealed class VirtualIrrigationEnvironment
{
    private readonly Random _random = new();

    public double SoilMoisture { get; private set; } = 55; // percent
    public double TemperatureC { get; private set; } = 20; // Celsius
    public double RainLevel { get; private set; } = 10;    // 0-100 scale

    /// <summary>
    /// Randomizes the environment slightly around the current values.
    /// </summary>
    public void Drift()
    {
        SoilMoisture = Clamp(SoilMoisture + NextDelta(5), 0, 100);
        TemperatureC = Clamp(TemperatureC + NextDelta(2), -10, 40);
        RainLevel = Clamp(RainLevel + NextDelta(15), 0, 100);
    }

    /// <summary>
    /// When sprinklers run, soil moisture should increase; rain should slowly decrease.
    /// </summary>
    public void ApplyIrrigationEffect(bool valveOpen)
    {
        if (valveOpen)
        {
            SoilMoisture = Clamp(SoilMoisture + 5 + _random.NextDouble() * 4, 0, 100);
        }
        else
        {
            // Evaporation dries the soil a bit over time
            SoilMoisture = Clamp(SoilMoisture - (0.5 + _random.NextDouble()), 0, 100);
        }
        RainLevel = Clamp(RainLevel - 2, 0, 100);
    }

    private double NextDelta(double maxMagnitude) =>
        ( _random.NextDouble() - 0.5 ) * 2 * maxMagnitude;

    private static double Clamp(double value, double min, double max) =>
        value < min ? min : (value > max ? max : value);
}
