namespace HyperBoostX.Windows;

// Conservative read-only adaptation of the V1.3 GPU family/profile classifier.
// PCI vendor ID takes precedence over marketing-name heuristics; no VRAM or
// integrated/dedicated inference from ambiguous model names.
public sealed record GpuIdentity(string Vendor,string Family,string Profile)
{
    public static GpuIdentity Classify(string? model,string? deviceId)
    {
        var name=model?.Trim()??"";
        bool Has(string value)=>name.Contains(value,StringComparison.OrdinalIgnoreCase);
        if(Has("Microsoft Basic Display"))return new("Microsoft","Basic Display Adapter","Generic safe mode");
        var pci=deviceId?.StartsWith(@"PCI\",StringComparison.OrdinalIgnoreCase)==true?deviceId.Split('&')[0]:"";
        var vendor=pci.ToUpperInvariant() switch
        {
            @"PCI\VEN_10DE"=>"NVIDIA",
            @"PCI\VEN_1002"=>"AMD",
            @"PCI\VEN_8086"=>"Intel",
            ""=>Has("NVIDIA")||Has("GeForce")?"NVIDIA":Has("AMD")||Has("Radeon")?"AMD":Has("Intel")?"Intel":"Unknown",
            _=>"Unknown"
        };
        return vendor switch
        {
            "NVIDIA" when Has("RTX")=>new(vendor,"GeForce RTX / RTX","Review NVIDIA gaming or creator settings"),
            "NVIDIA" when Has("GTX")=>new(vendor,"GeForce GTX","Review NVIDIA gaming settings"),
            "NVIDIA"=>new(vendor,"NVIDIA GPU","Review vendor settings"),
            "AMD" when Has("Radeon RX")=>new(vendor,"Radeon RX","Review AMD gaming settings"),
            "AMD" when Has("Vega")=>new(vendor,"Radeon Vega","Review AMD settings"),
            "AMD"=>new(vendor,"AMD GPU","Review vendor settings"),
            "Intel" when Has("Arc")=>new(vendor,"Intel Arc","Review Intel graphics settings"),
            "Intel" when Has("Iris Xe")=>new(vendor,"Intel Iris Xe","Conservative graphics settings"),
            "Intel" when Has("UHD")=>new(vendor,"Intel UHD","Conservative graphics settings"),
            "Intel"=>new(vendor,"Intel GPU","Review vendor settings"),
            _=>new("Unknown","Unknown","Generic safe mode")
        };
    }
}
