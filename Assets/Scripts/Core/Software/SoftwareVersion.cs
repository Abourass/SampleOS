using System;

/// <summary>
/// Handles software versions with support for various formats including letter suffixes
/// </summary>
public class SoftwareVersion : IComparable<SoftwareVersion>
{
  private int[] numericComponents;
  private string suffix = "";

  public SoftwareVersion(string version)
  {
    if (string.IsNullOrEmpty(version))
    {
      numericComponents = new int[] { 0, 0, 0, 0 };
      return;
    }

    // Extract letter suffix if any
    int lastNumericIndex = version.Length - 1;
    while (lastNumericIndex >= 0 && !char.IsDigit(version[lastNumericIndex]) && version[lastNumericIndex] != '.')
    {
      lastNumericIndex--;
    }

    string numericPart = version;
    if (lastNumericIndex < version.Length - 1)
    {
      suffix = version.Substring(lastNumericIndex + 1);
      numericPart = version.Substring(0, lastNumericIndex + 1);
    }

    // Parse the numeric parts
    string[] parts = numericPart.Split('.');
    numericComponents = new int[4]; // major.minor.patch.build

    for (int i = 0; i < parts.Length && i < 4; i++)
    {
      if (int.TryParse(parts[i], out int component))
      {
        numericComponents[i] = component;
      }
    }
  }

  public int CompareTo(SoftwareVersion other)
  {
    // Compare the numeric components
    for (int i = 0; i < 4; i++)
    {
      if (numericComponents[i] != other.numericComponents[i])
      {
        return numericComponents[i].CompareTo(other.numericComponents[i]);
      }
    }

    // If numeric components are equal, compare the suffixes
    if (string.IsNullOrEmpty(suffix) && string.IsNullOrEmpty(other.suffix))
      return 0;

    if (string.IsNullOrEmpty(suffix))
      return -1; // No suffix is less than any suffix

    if (string.IsNullOrEmpty(other.suffix))
      return 1;

    return suffix.CompareTo(other.suffix);
  }

  // Add comparison operators
  public static bool operator <(SoftwareVersion v1, SoftwareVersion v2)
  {
    return v1.CompareTo(v2) < 0;
  }

  public static bool operator >(SoftwareVersion v1, SoftwareVersion v2)
  {
    return v1.CompareTo(v2) > 0;
  }

  public static bool operator <=(SoftwareVersion v1, SoftwareVersion v2)
  {
    return v1.CompareTo(v2) <= 0;
  }

  public static bool operator >=(SoftwareVersion v1, SoftwareVersion v2)
  {
    return v1.CompareTo(v2) >= 0;
  }

  public static bool operator ==(SoftwareVersion v1, SoftwareVersion v2)
  {
    if (ReferenceEquals(v1, null))
      return ReferenceEquals(v2, null);
    return v1.CompareTo(v2) == 0;
  }

  public static bool operator !=(SoftwareVersion v1, SoftwareVersion v2)
  {
    return !(v1 == v2);
  }

  public override bool Equals(object obj)
  {
    if (obj is SoftwareVersion other)
      return CompareTo(other) == 0;
    return false;
  }

  public override int GetHashCode()
  {
    unchecked
    {
      int hash = 17;
      foreach (int component in numericComponents)
      {
        hash = hash * 31 + component;
      }
      hash = hash * 31 + (suffix?.GetHashCode() ?? 0);
      return hash;
    }
  }

  public override string ToString()
  {
    string version = $"{numericComponents[0]}.{numericComponents[1]}.{numericComponents[2]}";
    if (numericComponents[3] > 0)
      version += $".{numericComponents[3]}";
    if (!string.IsNullOrEmpty(suffix))
      version += suffix;
    return version;
  }
}
