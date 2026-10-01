using EFT;
using EFT.InventoryLogic;

namespace BOT_Light_Laser_Vision.Models;

public enum NVGQualityTier
{
    None = 0,
    Gen1_Soviet,
    Gen2_Commercial,
    Gen3_Military,
    Gen3_Panoramic,
    Thermal
}

public struct NVGProfile
{
    public NVGQualityTier Tier;
    public float FieldOfView;
    public float AmbientVisionDistance;
    public float MaxTransverseBeamRange;
    public float BlackoutDuration;
    public bool HasPeripheralSight;
    public bool IsThermal;

    public static NVGProfile CreateDefaultNakedEye()
    {
        return new NVGProfile
        {
            Tier = NVGQualityTier.None,
            FieldOfView = 90.0f,
            AmbientVisionDistance = 25.0f,
            MaxTransverseBeamRange = 0.0f,
            BlackoutDuration = 0.0f,
            HasPeripheralSight = true,
            IsThermal = false
        };
    }

    public static NVGProfile EvaluateBotNVG(BotOwner bot)
    {
        if (bot?.NightVision == null || !bot.NightVision.UsingNow)
        {
            return CreateDefaultNakedEye();
        }

        Player player = bot.GetPlayer;
        if (player?.InventoryController?.Inventory?.Equipment == null)
        {
            return CreateDefaultNakedEye();
        }

        // Check equipped headwear items for NVG model identification
        var headwear = player.InventoryController.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear)?.ContainedItem;
        string itemName = string.Empty;
        string templateId = string.Empty;

        if (headwear != null)
        {
            foreach (var item in headwear.GetAllItems())
            {
                if (item != null)
                {
                    string name = item.Template?.Name?.ToLower() ?? string.Empty;
                    string tpl = item.TemplateId.ToString();
                    if (name.Contains("nvg") || name.Contains("pvs") || name.Contains("gpnvg") || name.Contains("pnv") || name.Contains("n-15") || name.Contains("thermal") || name.Contains("t7"))
                    {
                        itemName = name;
                        templateId = tpl;
                        break;
                    }
                }
            }
        }

        // T-7 Thermal Goggles
        if (itemName.Contains("t7") || itemName.Contains("thermal") || templateId == "5a1eaa87fcdbcb001865f75e")
        {
            return new NVGProfile
            {
                Tier = NVGQualityTier.Thermal,
                FieldOfView = 50.0f,
                AmbientVisionDistance = 120.0f,
                MaxTransverseBeamRange = 0.0f,
                BlackoutDuration = 0.0f,
                HasPeripheralSight = false,
                IsThermal = true
            };
        }

        // GPNVG-18 (Panoramic Quad Tube)
        if (itemName.Contains("gpnvg") || templateId == "5c05580686f77416d042185c")
        {
            return new NVGProfile
            {
                Tier = NVGQualityTier.Gen3_Panoramic,
                FieldOfView = 97.0f,
                AmbientVisionDistance = 115.0f,
                MaxTransverseBeamRange = 100.0f,
                BlackoutDuration = 0.45f,
                HasPeripheralSight = false,
                IsThermal = false
            };
        }

        // PVS-14 (Monocular)
        if (itemName.Contains("pvs14") || itemName.Contains("pvs-14") || templateId == "57235b6f24597759bf5a30e1")
        {
            return new NVGProfile
            {
                Tier = NVGQualityTier.Gen3_Military,
                FieldOfView = 40.0f,
                AmbientVisionDistance = 90.0f,
                MaxTransverseBeamRange = 90.0f,
                BlackoutDuration = 0.85f,
                HasPeripheralSight = true,
                IsThermal = false
            };
        }

        // PVS-31A (Gen 3 Dual Tube)
        if (itemName.Contains("pvs31") || itemName.Contains("pvs-31"))
        {
            return new NVGProfile
            {
                Tier = NVGQualityTier.Gen3_Military,
                FieldOfView = 40.0f,
                AmbientVisionDistance = 95.0f,
                MaxTransverseBeamRange = 90.0f,
                BlackoutDuration = 0.70f,
                HasPeripheralSight = false,
                IsThermal = false
            };
        }

        // NVG-7 (Commercial Gen 2+)
        if (itemName.Contains("nvg7") || itemName.Contains("nvg-7"))
        {
            return new NVGProfile
            {
                Tier = NVGQualityTier.Gen2_Commercial,
                FieldOfView = 40.0f,
                AmbientVisionDistance = 65.0f,
                MaxTransverseBeamRange = 82.0f,
                BlackoutDuration = 1.20f,
                HasPeripheralSight = false,
                IsThermal = false
            };
        }

        // PNV-10T / N-15 (Soviet / Gen 1)
        if (itemName.Contains("pnv") || itemName.Contains("n-15") || itemName.Contains("n15") || templateId == "5c06665c86f7746316223522" || templateId == "5c06968386f77477b01a6135")
        {
            return new NVGProfile
            {
                Tier = NVGQualityTier.Gen1_Soviet,
                FieldOfView = 35.0f,
                AmbientVisionDistance = 45.0f,
                MaxTransverseBeamRange = 75.0f,
                BlackoutDuration = 1.50f,
                HasPeripheralSight = false,
                IsThermal = false
            };
        }

        // Generic fallback for active night vision
        return new NVGProfile
        {
            Tier = NVGQualityTier.Gen2_Commercial,
            FieldOfView = 40.0f,
            AmbientVisionDistance = 70.0f,
            MaxTransverseBeamRange = 80.0f,
            BlackoutDuration = 1.0f,
            HasPeripheralSight = false,
            IsThermal = false
        };
    }
}
