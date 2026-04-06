using Godot;
using System;
using System.Collections.Generic;

public static class FactoryPreviewPoolSupport
{
    public static void ClearChildren(Node3D? root)
    {
        if (root is null)
        {
            return;
        }

        foreach (var child in root.GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }
    }

    public static void EnsureMeshCapacity(
        Node3D? root,
        List<MeshInstance3D> meshes,
        int count,
        Func<int, MeshInstance3D> createMesh)
    {
        if (root is null)
        {
            return;
        }

        while (meshes.Count < count)
        {
            var mesh = createMesh(meshes.Count);
            root.AddChild(mesh);
            meshes.Add(mesh);
        }
    }

    public static void RefreshMeshGeometry(IReadOnlyList<MeshInstance3D> meshes, Action<MeshInstance3D> refreshMesh)
    {
        for (var index = 0; index < meshes.Count; index++)
        {
            refreshMesh(meshes[index]);
        }
    }

    public static void SetVisibleMeshCount(Node3D? root, IReadOnlyList<MeshInstance3D> meshes, int visibleCount)
    {
        if (root is null)
        {
            return;
        }

        for (var index = visibleCount; index < meshes.Count; index++)
        {
            meshes[index].Visible = false;
        }

        root.Visible = visibleCount > 0;
    }

    public static FactoryStructure EnsureGhostPreview(
        Node3D? root,
        List<FactoryStructure> ghosts,
        int index,
        BuildPrototypeKind kind,
        Func<BuildPrototypeKind, FactoryStructure> createGhost,
        string namePrefix)
    {
        if (root is null)
        {
            throw new InvalidOperationException("Ghost preview root is missing.");
        }

        if (index < ghosts.Count && ghosts[index].Kind == kind)
        {
            return ghosts[index];
        }

        if (index < ghosts.Count)
        {
            ghosts[index].QueueFree();
            ghosts.RemoveAt(index);
        }

        var ghost = createGhost(kind);
        ghost.Name = $"{namePrefix}_{index}_{kind}";
        root.AddChild(ghost);
        ghosts.Insert(index, ghost);
        return ghost;
    }
}
