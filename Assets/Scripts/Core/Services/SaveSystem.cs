using UnityEngine;

namespace SampleOS.Core.Services
{
  public static class SaveSystem
  {
    public static void Save(GameSaveData data)
    {
      // TODO: Implement actual save system
      Debug.Log("Game saved (stub)");
    }

    public static GameSaveData Load()
    {
      // TODO: Implement actual load system
      Debug.Log("Game loaded (stub)");
      return new GameSaveData();
    }
  }

  public class GameSaveData
  {
    public object worldState;
    public object playerState;
    public object networkState;
  }
}
