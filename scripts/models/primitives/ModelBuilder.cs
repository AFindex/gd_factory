using Godot;

namespace NetFactory.Models;

public interface IModelBuilder
{
    float CellSize { get; }
    Node3D Root { get; }

    MeshInstance3D AddBox(string name, Vector3 size, Color color, Vector3? position = null);
    MeshInstance3D AddBox(Node parent, string name, Vector3 size, Color color, Vector3? position = null);
    MeshInstance3D AddArmBox(Node parent, string name, Vector3 size, Color color, Vector3 position);
    MeshInstance3D AddCombatBox(string name, Vector3 size, Color color, Vector3 position);
    MeshInstance3D AddDisc(string name, float radius, float height, Color color, Vector3? position = null);
    MeshInstance3D AddDisc(Node parent, string name, float radius, float height, Color color, Vector3? position = null);
    MeshInstance3D AddCylinder(string name, float radius, float length, Color color, Vector3 position);
    MeshInstance3D AddCylinder(Node parent, string name, float radius, float length, Color color, Vector3 position);
    void ConfigureGlowMaterial(MeshInstance3D mesh, Color emissionColor, float emissionEnergy);
    void AddInteriorModuleShell(Node parent, string prefix, Vector3 shellSize, Color shellColor, Color trimColor, Vector3 position);
    void AddInteriorTray(Node parent, string prefix, Vector3 traySize, Color trayColor, Color railColor, Vector3 position);
    void AddInteriorIndicatorLight(Node parent, string name, Color color, Vector3 position, float size);
    void AddIndicatorLight(string name, Color color, Vector3 position, float size);
    void AddInteriorLabelPlate(Node parent, string prefix, string label, Color color, Vector3 position, float widthScale = 1.0f);
    void AddLabelPlate(string name, string label, Color color, Vector3 position, float cellSize, float widthScale);
    Node3D AddPivotNode(string name, Vector3 position);
    Node3D AddPivotNode(Node parent, string name, Vector3 position);
}
