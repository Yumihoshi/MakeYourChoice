using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
	/// <summary>
	/// 场景切换的唯一入口：以 Single 模式加载目标场景，当前所有已加载场景会被一并卸载。
	/// 只做校验与切场景，不负责淡出、音效或演出。
	/// </summary>
	public sealed class SceneLoadRequest : MonoBehaviour
	{
		[SerializeField, LabelText("目标场景名"), Tooltip("必须是 Build Settings 里登记过的场景名")]
		string sceneName = "Gallery";

		bool _requested;

		/// <summary>请求切到目标场景；重复调用只生效一次。</summary>
		public void Load()
		{
			if (_requested)
			{
				return;
			}

			if (string.IsNullOrWhiteSpace(sceneName))
			{
				Debug.LogError("SceneLoadRequest 的目标场景名为空", this);
				return;
			}

			if (!Application.CanStreamedLevelBeLoaded(sceneName))
			{
				Debug.LogError($"SceneLoadRequest 无法加载场景「{sceneName}」：请确认它已加入 Build Settings", this);
				return;
			}

			_requested = true;
			SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
		}
	}
}

