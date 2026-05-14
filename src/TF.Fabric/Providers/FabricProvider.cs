using TF.Fabric.Credentials;

namespace TF.Fabric.Providers;

public class FabricProvider(FabricOidcCredential credential, bool preview = true) : Provider(credential)
{
    [Terraform("preview")]
    public bool Preview { get; } = preview;

    public override string Name => "fabric";
}
