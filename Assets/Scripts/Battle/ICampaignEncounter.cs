using System;
namespace Alif.Battle
{
    public enum EncounterResult { Won, Cancelled }
    public interface ICampaignEncounter
    {
        event Action<EncounterResult> Finished;
        void SetPaused(bool paused);
        void Cancel();
    }
}
