using UnityEngine;
using Framework;
using Framework.Core;

namespace Watermelon
{
    public interface IClickableObject
    {
        public void OnObjectClicked();
        public bool CanBeClicked();
        public void OnClickBlocked();
    }

	public interface IClickableObjectRenderer
	{
		public void OnObjectClicked();
		public bool CanBeClicked();
		public void OnClickBlocked();
	}
}
