
#nullable enable

namespace Uchuhikoshi.U17CubemapGenerator
{
	public interface IU17CubemapGeneratorWindowTabView
	{
		void OnEnable();
		void OnDisable();
		void OnDestroy();
		void OnGUI();
		void OnUpdate(bool isTabActive);
	}
}
