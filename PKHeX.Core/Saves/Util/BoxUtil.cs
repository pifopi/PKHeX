using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using static PKHeX.Core.MessageStrings;

namespace PKHeX.Core;

/// <summary>
/// Contains extension methods for use with a <see cref="SaveFile"/>.
/// </summary>
public static class BoxUtil
{
    extension(SaveFile sav)
    {
        /// <summary>
        /// Dumps a folder of files to the <see cref="SaveFile"/>.
        /// </summary>
        /// <param name="path">Folder to store <see cref="PKM"/> files.</param>
        /// <param name="boxFolders">Option to save in child folders with the Box Name as the folder name.</param>
        /// <returns>-1 if aborted, otherwise the amount of files dumped.</returns>
        public int DumpBoxes(string path, bool boxFolders = false)
        {
            string pokemonList = "";
            pokemonList += $"National dex,Form,Regional dex,French name,English name,Inside raids Poke,Inside raids Beast,Inside raids Dream,Outside raids Poke,Outside raids Beast,Outside raids Dream\n";

            string filter = "[";
            bool first = true;

            if (!sav.HasBox)
                return -1;

            var boxData = sav.BoxData;
            int boxSlotCount = sav.BoxSlotCount;
            var ctr = 0;
            for (var slot = 0; slot < boxData.Count; slot++)
            {
                var pk = boxData[slot];
                var box = slot / boxSlotCount;
                if (pk.Species == 0 || !pk.Valid)
                    continue;

                var boxFolder = path;
                if (boxFolders)
                {
                    string boxName = sav is IBoxDetailName bn ? bn.GetBoxName(box) : BoxDetailNameExtensions.GetDefaultBoxName(box);
                    boxName = PathUtil.CleanFileName(boxName);
                    boxFolder = Path.Combine(path, boxName);
                    Directory.CreateDirectory(boxFolder);
                }

                var fileName = PathUtil.CleanFileName(pk.FileName);
                var fn = Path.Combine(boxFolder, fileName);
                if (File.Exists(fn))
                    continue;

                File.WriteAllBytes(fn, pk.DecryptedPartyData);
                ctr++;

                var basePath = "C:\\Users\\dotte\\Documents\\GitHub\\PokemonDatabase\\SV\\living dex shiny\\";
                var basePathInsideRaids = basePath + "inside raids\\";
                var filesPokeInsideRaids = Directory.EnumerateFiles(basePathInsideRaids + "poke\\", $"{pk.Species:0000}-{pk.Form:00}*", SearchOption.AllDirectories);
                var filesBeastInsideRaids = Directory.EnumerateFiles(basePathInsideRaids + "beast\\", $"{pk.Species:0000}-{pk.Form:00}*", SearchOption.AllDirectories);
                var filesDreamInsideRaids = Directory.EnumerateFiles(basePathInsideRaids + "dream\\", $"{pk.Species:0000}-{pk.Form:00}*", SearchOption.AllDirectories);
                var basePathOutsideRaids = basePath + "outside raids\\";
                var filesPokeOutsideRaids = Directory.EnumerateFiles(basePathOutsideRaids + "poke\\", $"{pk.Species:0000}-{pk.Form:00}*", SearchOption.AllDirectories);
                var filesBeastOutsideRaids = Directory.EnumerateFiles(basePathOutsideRaids + "beast\\", $"{pk.Species:0000}-{pk.Form:00}*", SearchOption.AllDirectories);
                var filesDreamOutsideRaids = Directory.EnumerateFiles(basePathOutsideRaids + "dream\\", $"{pk.Species:0000}-{pk.Form:00}*", SearchOption.AllDirectories);

                var french_name = SpeciesName.GetSpeciesNameGeneration(pk.Species, (int)LanguageID.French, pk.Format);
                var english_name = SpeciesName.GetSpeciesNameGeneration(pk.Species, (int)LanguageID.English, pk.Format);
                pokemonList += $"{pk.Species:0000},{pk.Form:00},{((PersonalInfo9SV)pk.PersonalInfo).DexPaldea},{french_name},{english_name}";
                pokemonList += $",{filesPokeInsideRaids.Count()},{filesBeastInsideRaids.Count()},{filesDreamInsideRaids.Count()}";
                pokemonList += $",{filesPokeOutsideRaids.Count()},{filesBeastOutsideRaids.Count()},{filesDreamOutsideRaids.Count()}\n";

                if (!filesPokeInsideRaids.Any() &&
                    !filesBeastInsideRaids.Any() &&
                    !filesDreamInsideRaids.Any() &&
                    !filesPokeOutsideRaids.Any() &&
                    !filesBeastOutsideRaids.Any() &&
                    !filesDreamOutsideRaids.Any())
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        filter += ",";
                    }
                    filter += $"{{\"Name\":\"{pk.Species:0000} {french_name} {english_name} shiny\",\"Species\":{pk.Species},\"Form\":{pk.Form},\"Shiny\":true,\"Enabled\":true}}";
                }
            }

            File.WriteAllText("pokemon_list.csv", pokemonList);

            filter += "]";
            File.WriteAllText("filters.json", filter);

            return ctr;
        }

        /// <summary>
        /// Dumps the <see cref="SaveFile.BoxData"/> to a folder with individual decrypted files.
        /// </summary>
        /// <param name="path">Folder to store <see cref="PKM"/> files.</param>
        /// <param name="currentBox">Box contents to be dumped.</param>
        /// <returns>-1 if aborted, otherwise the amount of files dumped.</returns>
        public int DumpBox(string path, int currentBox)
        {
            if (!sav.HasBox)
                return -1;

            var boxData = sav.BoxData;
            int boxSlotCount = sav.BoxSlotCount;
            var ctr = 0;
            for (var slot = 0; slot < boxData.Count; slot++)
            {
                var pk = boxData[slot];
                var box = slot / boxSlotCount;
                if (pk.Species == 0 || !pk.Valid || box != currentBox)
                    continue;

                var fileName = Path.Combine(path, PathUtil.CleanFileName(pk.FileName));
                if (File.Exists(fileName))
                    continue;

                File.WriteAllBytes(fileName, pk.DecryptedPartyData);
                ctr++;
            }
            return ctr;
        }

        /// <summary>
        /// Loads a folder of files to the <see cref="SaveFile"/>.
        /// </summary>
        /// <param name="path">Folder to load <see cref="PKM"/> files from. Files are only loaded from the top directory.</param>
        /// <param name="result">Result message from the method.</param>
        /// <param name="boxStart">First box to start loading to. All prior boxes are not modified.</param>
        /// <param name="boxClear">Instruction to clear boxes after the starting box.</param>
        /// <param name="overwrite">Overwrite existing full slots. If true, will only overwrite empty slots.</param>
        /// <param name="settings">Bypass option to not modify <see cref="PKM"/> properties when setting to Save File.</param>
        /// <param name="all">Enumerate all files even in sub-folders.</param>
        /// <returns>Count of files imported.</returns>
        public int LoadBoxes(string path, out string result, int boxStart = 0, bool boxClear = false, bool overwrite = false, EntityImportSettings settings = default, bool all = false)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            { result = MsgSaveBoxExportPathInvalid; return -1; }

            var option = all ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.EnumerateFiles(path, "*.*", option);
            return sav.LoadBoxes(files, out result, boxStart, boxClear, overwrite, settings);
        }

        /// <summary>
        /// Loads a folder of files to the <see cref="SaveFile"/>.
        /// </summary>
        /// <param name="files">Files to load <see cref="PKM"/> files from.</param>
        /// <param name="result">Result message from the method.</param>
        /// <param name="boxStart">First box to start loading to. All prior boxes are not modified.</param>
        /// <param name="boxClear">Instruction to clear boxes after the starting box.</param>
        /// <param name="overwrite">Overwrite existing full slots. If true, will only overwrite empty slots.</param>
        /// <param name="settings">Bypass option to not modify <see cref="PKM"/> properties when setting to Save File.</param>
        /// <returns>Count of files imported.</returns>
        public int LoadBoxes(IEnumerable<string> files, out string result, int boxStart = 0, bool boxClear = false, bool overwrite = false, EntityImportSettings settings = default)
        {
            var pks = GetPossiblePKMsFromPaths(sav, files);
            return sav.LoadBoxes(pks, out result, boxStart, boxClear, overwrite, settings);
        }

        /// <summary>
        /// Loads a folder of files to the <see cref="SaveFile"/>.
        /// </summary>
        /// <param name="encounters">Encounters to create <see cref="PKM"/> files from.</param>
        /// <param name="result">Result message from the method.</param>
        /// <param name="boxStart">First box to start loading to. All prior boxes are not modified.</param>
        /// <param name="boxClear">Instruction to clear boxes after the starting box.</param>
        /// <param name="overwrite">Overwrite existing full slots. If true, will only overwrite empty slots.</param>
        /// <param name="settings">Bypass option to not modify <see cref="PKM"/> properties when setting to Save File.</param>
        /// <returns>Count of files imported.</returns>
        public int LoadBoxes(IEnumerable<IEncounterConvertible> encounters, out string result, int boxStart = 0, bool boxClear = false, bool overwrite = false, EntityImportSettings settings = default)
        {
            var pks = encounters.Select(z => z.ConvertToPKM(sav));
            return sav.LoadBoxes(pks, out result, boxStart, boxClear, overwrite, settings);
        }

        /// <summary>
        /// Loads a folder of files to the <see cref="SaveFile"/>.
        /// </summary>
        /// <param name="pks">Unconverted <see cref="PKM"/> objects to load.</param>
        /// <param name="result">Result message from the method.</param>
        /// <param name="boxStart">First box to start loading to. All prior boxes are not modified.</param>
        /// <param name="boxClear">Instruction to clear boxes after the starting box.</param>
        /// <param name="overwrite">Overwrite existing full slots. If true, will only overwrite empty slots.</param>
        /// <param name="settings">Bypass option to not modify <see cref="PKM"/> properties when setting to Save File.</param>
        /// <returns>True if any files are imported.</returns>
        public int LoadBoxes(IEnumerable<PKM> pks, out string result, int boxStart = 0, bool boxClear = false, bool overwrite = false, EntityImportSettings settings = default)
        {
            if (!sav.HasBox)
            { result = MsgSaveBoxFailNone; return -1; }

            var compat = sav.GetCompatible(pks);
            if (boxClear)
                sav.ClearBoxes(boxStart);

            int ctr = sav.ImportPKMs(compat, overwrite, boxStart, settings);
            if (ctr <= 0)
            {
                result = MsgSaveBoxImportNoFiles;
                return -1;
            }

            result = string.Format(MsgSaveBoxImportSuccess, ctr);
            return ctr;
        }
    }

    public static IEnumerable<PKM> GetPKMsFromPaths(IEnumerable<string> files, EntityContext generation)
    {
        foreach (var f in files)
        {
            var fi = new FileInfo(f);
            if (!fi.Exists)
                continue;
            if (!EntityDetection.IsSizePlausible(fi.Length))
                continue;
            var data = File.ReadAllBytes(f);
            var prefer = EntityFileExtension.GetContextFromExtension(fi.Extension, generation);
            var pk = EntityFormat.GetFromBytes(data, prefer);
            if (pk?.Species is > 0)
                yield return pk;
        }
    }

    private static IEnumerable<PKM> GetPossiblePKMsFromPaths(SaveFile sav, IEnumerable<string> files)
    {
        foreach (var f in files)
        {
            var obj = FileUtil.GetSupportedFile(f, sav);
            switch (obj)
            {
                case PKM pk:
                    yield return pk;
                    break;
                case MysteryGift {IsEntity: true} g:
                    yield return g.ConvertToPKM(sav);
                    break;
                case IEncounterInfo g when g.Species != 0:
                    yield return g.ConvertToPKM(sav);
                    break;
                case IPokeGroup g:
                    foreach (var p in g.Contents)
                        yield return p;
                    break;
                case IEnumerable<PKM> contents:
                    foreach (var pk in contents)
                        yield return pk;
                    break;
            }
        }
    }

    /// <summary>
    /// Gets box names for all boxes in the save file.
    /// </summary>
    /// <param name="sav"><see cref="SaveFile"/> that box names are being dumped for.</param>
    /// <returns>Returns default English box names in the event the save file does not have names (not exportable), or fails to return a box name.</returns>
    public static string[] GetBoxNames(SaveFile sav)
    {
        var count = sav.BoxCount;
        if (count == 0)
            return [];
        var result = new string[count];
        GetBoxNames(sav, result);
        return result;
    }

    private static void GetBoxNames(SaveFile sav, Span<string> result)
    {
        if (!sav.State.Exportable || sav is not IBoxDetailNameRead r)
        {
            for (int i = 0; i < result.Length; i++)
                result[i] = BoxDetailNameExtensions.GetDefaultBoxName(i);
            return;
        }

        for (int i = 0; i < result.Length; i++)
        {
            try { result[i] = r.GetBoxName(i); }
            catch { result[i] = BoxDetailNameExtensions.GetDefaultBoxName(i); }
        }
    }
}
