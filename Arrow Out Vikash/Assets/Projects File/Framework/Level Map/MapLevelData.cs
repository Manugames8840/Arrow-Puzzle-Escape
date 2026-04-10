using UnityEngine;
using Framework;
using Framework.Core;



namespace Watermelon
{
    [System.Serializable]
    public class MapLevelData
    {
        [SerializeField] LevelType levelType;
        public LevelType LevelType => levelType;

        [SerializeField] GameObject prefab;
        public GameObject Prefab => prefab;

        [SerializeField] string lable;
        public string Lable => lable;

        [Space]
        [SerializeField] Sprite icon;
        public Sprite Icon => icon;

		[SerializeField] Material backgroundMaterial;
		public Material BackMaterial => backgroundMaterial;

		[SerializeField] float iconScale = 1.0f;
        public float IconScale => iconScale;

        [SerializeField] Color iconColor = Color.white;
        public Color IconColor => iconColor;

        [SerializeField] Color textColor = Color.white;
        public Color TextColor => textColor;

        private Pool pool;
        public Pool Pool => pool;

        public void Init()
        {
            pool = new Pool(prefab, $"map_{levelType}");
        }

        public void Destroy()
        {
            PoolManager.DestroyPool(pool);
        }
    }
}
