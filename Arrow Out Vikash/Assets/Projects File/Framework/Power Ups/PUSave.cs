using Framework;
using Framework.Core;


namespace Watermelon
{
    public class PUSave : ISaveObject
    {
        public int Amount = -1;
        public bool IsUnlocked = false;
        public int PurchaseUseableVideo = -1;

        public void Flush()
        {

        }
    }
}
