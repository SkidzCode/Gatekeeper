using GateKeeper.Server.Models.Resources;

namespace GateKeeper.Server.Interface;

public interface IResourceService
{
    /// <summary>
    /// Lists all entries in the specified resource file.
    /// </summary>
    /// <param name="resourceFileName">Name of the resource file without extension.</param>
    /// <returns>A list of resource entries.</returns>
    List<ResourceEntry> ListEntries(string resourceFileName);

    /// <summary>
    /// Adds a new entry to the resource file.
    /// </summary>
    /// <param name="resourceFileName">Name of the resource file without extension.</param>
    /// <param name="request">Entry data.</param>
    void AddEntry(string resourceFileName, AddResourceEntryRequest request);

    /// <summary>
    /// Updates an existing entry in the resource file.
    /// </summary>
    /// <param name="resourceFileName">Name of the resource file without extension.</param>
    /// <param name="key">Key of the resource to update.</param>
    /// <param name="request">Updated values.</param>
    void UpdateEntry(string resourceFileName, string key, UpdateResourceEntryRequest request);
}
