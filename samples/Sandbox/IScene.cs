using NImpeller;

namespace Sandbox;

public class SceneParameters
{
    public int Complexity { get; set; } = 12;
    public int Width { get; set; }
    public int Height { get; set; }
}

public interface IScene
{
    void Render(ImpellerContext context, ImpellerDisplayListBuilder scene, SceneParameters sceneParameters);
}