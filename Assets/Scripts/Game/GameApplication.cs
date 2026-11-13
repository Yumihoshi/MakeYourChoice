using UnityEngine;

namespace Game
{
	/// <summary>
	/// 应用级操作。统一处理编辑器与真机下的差异。
	/// </summary>
	public static class GameApplication
	{
		/// <summary>
		/// 退出游戏：编辑器内停止 Play 模式，真机上退出应用。
		/// </summary>
		public static void Quit()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}
	}
}
