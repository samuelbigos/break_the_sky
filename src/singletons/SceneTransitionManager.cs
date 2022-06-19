using System;

public class SceneTransitionManager : Singleton<SceneTransitionManager>
{
    public static Action OnSceneTransitionInitiated;

    public void RequestReloadCurrentScene()
    {
        GetTree().ReloadCurrentScene();
        OnSceneTransitionInitiated?.Invoke();
    }
}