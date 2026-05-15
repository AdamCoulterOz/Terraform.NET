namespace TF.PowerPlatform.Providers;

public class PowerPlatformProvider(Credential credential) : Provider(credential)
{
    public override string Name => "power-platform";
    public override string Source => "microsoft/power-platform";
}
