
#nullable enable

namespace Uchuhikoshi.U17CubemapGenerator
{
	public abstract class U17CubemapGeneratorWindowTabBase : IU17CubemapGeneratorWindowTabView
	{
		U17CubemapGeneratorWindowContext _context = null!;
		protected U17CubemapGeneratorWindowContext context => _context;

		IU17CubemapGeneratorWindow _window = null!;
		protected IU17CubemapGeneratorWindow window => _window;

		protected U17CubemapGeneratorWindowTabBase(U17CubemapGeneratorWindowContext context_, IU17CubemapGeneratorWindow window_)
		{
			_context = context_;
			_window = window_;
		}

		public virtual void OnEnable() {}
		public virtual void OnDisable() {}
		public virtual void OnDestroy()
		{
			_context = null!;
			_window = null!;
		}
		public virtual void OnUpdate(bool isTabActive) {}
		public virtual void OnGUI() {}
	}
}
