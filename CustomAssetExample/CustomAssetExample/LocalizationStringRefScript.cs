using CustomAssetExample.SharedData;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Engine;

namespace CustomAssetExample
{
    public class LocalizationStringRefScript : SyncScript
    {
        private LocalizationStringDefinition _localizationString;

        /**
         * This will hold a reference to our custom asset.
         * This can be set up in Game Studio after attaching this script to an entity.
         */
        public UrlReference<LocalizationStringDefinition> LocalizationStringUrl { get; set; }

        public override void Start()
        {
            _localizationString = Content.Load(LocalizationStringUrl);
        }

        public override void Update()
        {
            int y = 15;

            PrintLine($"Custom Asset Example:");
            if (LocalizationStringUrl is null || _localizationString is null)
            {
                PrintLine($"LocalizationString is not set!");
                return;
            }

            PrintLine($"English: {_localizationString?.English}");
            PrintLine($"French : {_localizationString?.French}");
            PrintLine($"German : {_localizationString?.German}");

            void PrintLine(string line)
            {
                DebugText.Print(line, new Int2(x: 15, y: y));

                y += 20;
            }
        }
    }
}
