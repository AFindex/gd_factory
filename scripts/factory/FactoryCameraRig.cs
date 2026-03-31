using Godot;

public partial class FactoryCameraRig : Node3D
{
    private const float CameraDistance = FactoryConstants.CameraDefaultZoom;
    private const float FocusLerpSpeed = 8.0f;

    private Node3D? _pivot;
    private Camera3D? _camera;
    private Vector2 _currentPosition;
    private Vector2 _targetPosition;
    private Vector2 _minBounds;
    private Vector2 _maxBounds;
    private float _zoom = FactoryConstants.CameraDefaultZoom;

    public Camera3D Camera => _camera!;
    public bool AllowPanInput { get; set; } = true;
    public bool AllowZoomInput { get; set; } = true;

    public override void _Ready()
    {
        Name = "CameraRig";
        _pivot = new Node3D { Name = "PitchPivot" };
        _camera = new Camera3D
        {
            Name = "FactoryCamera",
            Current = true,
            Far = 400.0f,
            Near = 0.1f,
            Projection = Camera3D.ProjectionType.Orthogonal,
            Size = _zoom
        };

        AddChild(_pivot);
        _pivot.AddChild(_camera);

        ApplyTransformState();
    }

    public void ConfigureBounds(Vector2 minBounds, Vector2 maxBounds)
    {
        _minBounds = minBounds;
        _maxBounds = maxBounds;
        _targetPosition = _targetPosition.Clamp(_minBounds, _maxBounds);
        _currentPosition = _targetPosition;
        ApplyTransformState();
    }

    public void FocusWorldPosition(Vector3 worldPosition)
    {
        _targetPosition = new Vector2(worldPosition.X, worldPosition.Z).Clamp(_minBounds, _maxBounds);
        ApplyTransformState();
    }

    public void FocusWorldPositionInViewport(Vector3 worldPosition, Vector2 screenPosition)
    {
        if (!TryProjectScreenToPlane(screenPosition, worldPosition.Y, out var projectedWorldPosition))
        {
            FocusWorldPosition(worldPosition);
            return;
        }

        var delta = new Vector2(
            worldPosition.X - projectedWorldPosition.X,
            worldPosition.Z - projectedWorldPosition.Z);
        _targetPosition = (_targetPosition + delta).Clamp(_minBounds, _maxBounds);
        ApplyTransformState();
    }

    public override void _Process(double delta)
    {
        var input = Vector2.Zero;

        if (AllowPanInput && Input.IsActionPressed("camera_pan_left"))
        {
            input.X -= 1.0f;
        }

        if (AllowPanInput && Input.IsActionPressed("camera_pan_right"))
        {
            input.X += 1.0f;
        }

        if (AllowPanInput && Input.IsActionPressed("camera_pan_up"))
        {
            input.Y -= 1.0f;
        }

        if (AllowPanInput && Input.IsActionPressed("camera_pan_down"))
        {
            input.Y += 1.0f;
        }

        if (input != Vector2.Zero)
        {
            input = input.Normalized();
            _targetPosition += new Vector2(input.X, input.Y) * 18.0f * (float)delta;
            _targetPosition = _targetPosition.Clamp(_minBounds, _maxBounds);
        }

        if (AllowZoomInput && Input.IsActionJustPressed("camera_zoom_in"))
        {
            _zoom = Mathf.Clamp(_zoom - 2.0f, FactoryConstants.CameraMinZoom, FactoryConstants.CameraMaxZoom);
        }

        if (AllowZoomInput && Input.IsActionJustPressed("camera_zoom_out"))
        {
            _zoom = Mathf.Clamp(_zoom + 2.0f, FactoryConstants.CameraMinZoom, FactoryConstants.CameraMaxZoom);
        }

        _currentPosition = _currentPosition.Lerp(_targetPosition, 1.0f - Mathf.Exp(-FocusLerpSpeed * (float)delta));
        ApplyTransformState();
    }

    public bool TryProjectMouseToPlane(Vector2 mousePosition, out Vector3 worldPosition)
    {
        return TryProjectScreenToPlane(mousePosition, 0.0f, out worldPosition);
    }

    public bool TryProjectScreenToPlane(Vector2 screenPosition, float planeY, out Vector3 worldPosition)
    {
        worldPosition = Vector3.Zero;

        if (_camera is null)
        {
            return false;
        }

        var rayOrigin = _camera.ProjectRayOrigin(screenPosition);
        var rayDirection = _camera.ProjectRayNormal(screenPosition);

        if (Mathf.Abs(rayDirection.Y) < 0.001f)
        {
            return false;
        }

        var distance = (planeY - rayOrigin.Y) / rayDirection.Y;
        if (distance < 0.0f)
        {
            return false;
        }

        worldPosition = rayOrigin + rayDirection * distance;
        return true;
    }

    private void ApplyTransformState()
    {
        Position = new Vector3(_currentPosition.X, 0.0f, _currentPosition.Y);
        Rotation = Vector3.Zero;

        if (_pivot is not null)
        {
            _pivot.RotationDegrees = new Vector3(FactoryConstants.CameraPitchDegrees, 0.0f, 0.0f);
        }

        if (_camera is not null)
        {
            _camera.Position = new Vector3(0.0f, 0.0f, CameraDistance);
            _camera.Size = _zoom;
        }
    }
}
