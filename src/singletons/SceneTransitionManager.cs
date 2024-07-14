using System;

public partial class SceneTransitionManager : Singleton<SceneTransitionManager>
{
    public static Action OnSceneTransitionInitiated;

    public void RequestReloadCurrentScene()
    {
        GetTree().ReloadCurrentScene();
        OnSceneTransitionInitiated?.Invoke();
    }
}