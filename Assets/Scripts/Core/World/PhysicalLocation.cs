using System.Collections.Generic;
using UnityEngine;

namespace SampleOS.Core.World
{
  /// <summary>
  /// A physical place the player can visit
  /// </summary>
  public class PhysicalLocation
  {
    public string LocationId { get; set; }
    public string LocationName { get; set; }
    public LocationType Type { get; set; }

    // Which devices are physically here?
    public List<string> DeviceIds { get; set; } = new List<string>();

    // 3D world position
    public Vector3 WorldPosition { get; set; }

    // Story/quest data
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    public enum LocationType
    {
      Apartment,
      Office,
      Cafe,
      DataCenter,
      Store,
      Bank,
      Hospital,
      University,
      Street,
      Industrial
    }
  }
}
