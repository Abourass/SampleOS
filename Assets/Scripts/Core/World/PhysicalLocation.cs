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

    // 3D world position
    public Vector3 WorldPosition { get; set; }

    // Story/quest data
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        // NEW: City reference
    public string CityId { get; set; }
    
    // NEW: Devices physically at this location (optional - registry is source of truth)
    public List<string> DeviceIds { get; set; } = new List<string>();

    // Distance calculation
    public float DistanceTo(PhysicalLocation other)
    {
      if (other == null) return float.MaxValue;
      return Vector3.Distance(WorldPosition, other.WorldPosition);
    }

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
