using System;

using TFlex;

namespace TFlexNF
{
    /// <summary>
	/// Для создания приложения необходимо иметь класс, порождённый от PluginFactory
	/// </summary>
	public class Factory : PluginFactory
    {
        /// <summary>
        /// Необходимо также переопределить данный метод для создания объекта
        /// </summary>
        public override Plugin CreateInstance() => new PluginInstance(this);

        /// <summary>
		/// Уникальный GUID приложения. Он должен быть обязательно разным у разных приложений
		/// </summary>
        public override Guid ID => new Guid("{3a088183-550d-495d-870e-2eecb287adae}");

        /// <summary>
		/// Имя приложения
		/// </summary>
		public override string Name => "Nesting Factory";
    };
}
