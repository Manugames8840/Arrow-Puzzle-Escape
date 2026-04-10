using System;
using UnityEngine;
using Framework;
using Framework.Core;



namespace Watermelon
{
	public abstract class PUBehavior : MonoBehaviour
	{
		protected PUSettings settings;
		public PUSettings Settings => settings;

		private bool isBusy;
		public bool IsBusy
		{
			get => isBusy;
			protected set
			{
				isBusy = value;
				isDirty = true;
			}
		}

		private bool isSelected;
		public bool IsSelected
		{
			get => isSelected;
			protected set
			{
				isSelected = value;
				isDirty = true;

				SelectStateChanged?.Invoke(isSelected);
			}
		}

		private bool isActivated;
		public bool IsActivated
		{
			get => isActivated;
			protected set
			{
				isActivated = value;
				SelectStateChanged?.Invoke(isSelected);
			}
		}

		protected bool isDirty = true;
		public bool IsDirty => isDirty;

		public event SimpleBoolCallback SelectStateChanged;

		public void InitialiseSettings(PUSettings settings)
		{
			this.settings = settings;
		}

		public abstract void Init();
		public abstract bool Activate();

		public virtual bool ApplyToElement(IClickableObject clickableObject, Vector3 clickPosition) { return false; }

		public virtual void OnLevelLoaded() { }
		public virtual void OnLevelEnded() { }

		public virtual void OnSelected()
		{
			isSelected = true;
			isDirty = true;

			SelectStateChanged?.Invoke(true);
		}

		public virtual void OnDeselected()
		{
			isSelected = false;
			isDirty = true;

			SelectStateChanged?.Invoke(false);
		}

		public virtual bool IsActive() => true;
		public virtual bool IsSelectable() => false;
		public virtual bool IsEnableDisable() => false;
		public virtual void DisablePowerUp()
		{
			
		}
		public virtual string GetFloatingMessage()
		{
			return settings.FloatingMessage;
		}

		public virtual PUTimer GetTimer()
		{
			return null;
		}

		public virtual void OnTimerTick()
		{

		}

		public virtual void ResetBehavior()
		{

		}

		public void SetDirty()
		{
			isDirty = true;
		}

		public void OnRedrawn()
		{
			isDirty = false;
		}
	}
}
