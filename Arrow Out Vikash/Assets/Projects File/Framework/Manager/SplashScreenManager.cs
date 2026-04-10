using Framework;
using UnityEngine;

namespace ColorBlockJam
{
	public class SplashScreenManager : MonoBehaviour
	{
		// Start is called once before the first execution of Update after the MonoBehaviour is created
		void Start()
		{
			LoadingEvents.RequestSceneLoad("MainScreen");
		}
	}
}
