using Newtonsoft.Json.Linq;

namespace NeonTrader.Core.Interfaces;

/// <summary>
/// Interface for components that can be serialized and saved to disk.
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// Unique identifier for this saveable component
    /// </summary>
    string SaveId { get; }

    /// <summary>
    /// Serialize the component to a JSON object
    /// </summary>
    /// <returns>JObject containing the serialized state</returns>
    JObject Serialize();

    /// <summary>
    /// Deserialize the component from a JSON object
    /// </summary>
    /// <param name="data">JObject containing the serialized state</param>
    void Deserialize(JObject data);

    /// <summary>
    /// Get the version of the save format for this component
    /// </summary>
    int SaveVersion { get; }
}