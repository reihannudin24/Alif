using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Alif.Dialogue;

namespace Alif.EditorTools
{
    /// <summary>
    /// Mengimpor file dialog eksternal berbasis JSON dari folder Data/Dialogue/
    /// menjadi ScriptableObject DialogueData di Assets/ScriptableObjects/Dialogue/.
    /// </summary>
    public static class AlifDialogueImporter
    {
        private const string DialogueJsonFolder = "Data/Dialogue";
        private const string OutputFolder = "Assets/ScriptableObjects/Dialogue";

        [Serializable]
        private class DialogueJsonWrapper
        {
            public string dialogueId;
            public string onCompleteEventId;
            public List<DialogueLineJson> lines = new List<DialogueLineJson>();
        }

        [Serializable]
        private class DialogueLineJson
        {
            public string speakerName;
            public string text;
            public int nextLineIndex = -1;
            public List<DialogueChoiceJson> choices = new List<DialogueChoiceJson>();
        }

        [Serializable]
        private class DialogueChoiceJson
        {
            public string choiceText;
            public int nextLineIndex = -1;
            public int affinityChange;
            public float financialLogicChange;
            public float shariaComplianceChange;
            public float balanceCorrection;
            public string eventId;
            public string outcomeText;
        }

        [MenuItem("Alif/Dialogue/Import External Dialogue JSON")]
        public static void ImportAllDialogueJson()
        {
            if (!Directory.Exists(DialogueJsonFolder))
            {
                Debug.LogWarning($"[AlifDialogueImporter] Folder '{DialogueJsonFolder}' tidak ditemukan.");
                return;
            }

            if (!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
                AssetDatabase.Refresh();
            }

            string[] jsonFiles = Directory.GetFiles(DialogueJsonFolder, "*.json", SearchOption.AllDirectories);
            int importedCount = 0;

            foreach (string filePath in jsonFiles)
            {
                try
                {
                    string jsonContent = File.ReadAllText(filePath);
                    var wrapper = JsonUtility.FromJson<DialogueJsonWrapper>(jsonContent);

                    if (wrapper == null || string.IsNullOrEmpty(wrapper.dialogueId))
                    {
                        Debug.LogWarning($"[AlifDialogueImporter] File '{filePath}' tidak memiliki dialogueId yang valid.");
                        continue;
                    }

                    string assetPath = $"{OutputFolder}/DialogueData_{wrapper.dialogueId}.asset";
                    var data = AssetDatabase.LoadAssetAtPath<DialogueData>(assetPath);
                    bool isNew = false;

                    if (data == null)
                    {
                        data = ScriptableObject.CreateInstance<DialogueData>();
                        isNew = true;
                    }
                    else
                    {
                        data.Lines.Clear();
                    }

                    data.OnCompleteEventId = wrapper.onCompleteEventId;

                    if (wrapper.lines != null)
                    {
                        foreach (var lineJson in wrapper.lines)
                        {
                            var line = new DialogueLine
                            {
                                SpeakerName = lineJson.speakerName,
                                Text = lineJson.text,
                                NextLineIndex = lineJson.nextLineIndex,
                                Choices = new List<DialogueChoice>()
                            };

                            if (lineJson.choices != null)
                            {
                                foreach (var choiceJson in lineJson.choices)
                                {
                                    line.Choices.Add(new DialogueChoice
                                    {
                                        ChoiceText = choiceJson.choiceText,
                                        NextLineIndex = choiceJson.nextLineIndex,
                                        AffinityChange = choiceJson.affinityChange,
                                        FinancialLogicChange = choiceJson.financialLogicChange,
                                        ShariaComplianceChange = choiceJson.shariaComplianceChange,
                                        BalanceCorrection = choiceJson.balanceCorrection,
                                        EventId = choiceJson.eventId,
                                        OutcomeText = choiceJson.outcomeText
                                    });
                                }
                            }

                            data.Lines.Add(line);
                        }
                    }

                    EditorUtility.SetDirty(data);
                    if (isNew)
                    {
                        AssetDatabase.CreateAsset(data, assetPath);
                    }

                    importedCount++;
                    Debug.Log($"[AlifDialogueImporter] Berhasil mengimpor dialog '{wrapper.dialogueId}' -> {assetPath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AlifDialogueImporter] Gagal mengimpor '{filePath}': {ex.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AlifDialogueImporter] Selesai. Total {importedCount} file dialog berhasil diimpor.");
        }
    }
}
