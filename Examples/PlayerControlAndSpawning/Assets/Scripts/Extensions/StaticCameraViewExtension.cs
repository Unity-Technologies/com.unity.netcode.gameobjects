public class StaticCameraViewExtension : CameraViewExtension
{
    public static CameraViewExtension Instance;

    protected override void OnInitialize()
    {
        Instance = this;
        base.OnInitialize();
    }
}
