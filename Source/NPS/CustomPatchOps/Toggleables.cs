using System.Xml;
using Verse;

namespace TKKN_NPS;

public class Toggleables : PatchOperation
{

    public string setting;
    public PatchOperation patchOp;
    
    
    protected override bool ApplyWorker(XmlDocument xml)
    {
        if (BiomeSettings.GetActiveSettings(setting)) {
            return patchOp.Apply(xml);
        }

        return true;
    }
}