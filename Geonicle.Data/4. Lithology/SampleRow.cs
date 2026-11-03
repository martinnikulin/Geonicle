using Data.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Geonicle.Data.Services
{
    /// <summary>
    /// Строка грида «Пробы». Правки идут в SampleDef (таблица) и в связь пробы с выбранным пластопересечением (HSSamps);
    /// колонки Ad только для чтения: их считает представление Samples (из Assays), в коде эта логика не дублируется.
    /// </summary>
    public class SampleRow : INotifyPropertyChanged
    {
        private Hssamp? link;

        /// <summary>Новая проба (строку добавил грид): значения по умолчанию, как у столбцов БД.</summary>
        public SampleRow()
            : this(new SampleDef { Show = true, AdIndex = 1 })
        {
        }

        public SampleRow(SampleDef def)
        {
            Def = def;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public SampleDef Def { get; }

        /// <summary>Порядковый номер по глубине среди показанных проб (вычисляется экраном).</summary>
        public int N { get; private set; }

        public int SampleId => Def.SampleId;

        public bool Bad
        {
            get => Def.Bad;
            set { Def.Bad = value; Notify(); }
        }

        public string? Nsample
        {
            get => Def.Nsample;
            set { Def.Nsample = value; Notify(); }
        }

        public decimal DepthFrom
        {
            get => Def.DepthFrom;
            set { Def.DepthFrom = value; Notify(); }
        }

        public decimal DepthTo
        {
            get => Def.DepthTo;
            set { Def.DepthTo = value; Notify(); }
        }

        /// <summary>Какой анализ принимается (код — Dictionary.N, раздел «Ad»).</summary>
        public int AdIndex
        {
            get => Def.AdIndex;
            set { Def.AdIndex = value; Notify(); }
        }

        /// <summary>Принята ли проба в выбранном пластопересечении (HSSamps.Accepted); без связи — false и изменить нельзя.</summary>
        public bool Accepted
        {
            get => link?.Accepted ?? false;
            set
            {
                if (link == null || link.Accepted == value)
                    return;
                link.Accepted = value;
                Notify();
            }
        }

        /// <summary>Проба связана с выбранным пластопересечением.</summary>
        public bool HasSeamLink => link != null;

        public double? Ad { get; private set; }

        public double? AdFloat { get; private set; }

        public double? AdGbs { get; private set; }

        public double? AdGeophys { get; private set; }

        public double? AdSeam { get; private set; }

        internal void SetSeam(int? holeSeamId)
        {
            link = holeSeamId == null ? null : Def.Hssamps.FirstOrDefault(h => h.HoleSeamId == holeSeamId);
            Notify(nameof(Accepted));
            Notify(nameof(HasSeamLink));
        }

        internal void SetN(int n)
        {
            if (N == n)
                return;
            N = n;
            Notify(nameof(N));
        }

        /// <summary>Значения Ad из представления Samples (null — проба не найдена в представлении).</summary>
        internal void SetValues(Sample? values)
        {
            Ad = values?.Ad;
            AdFloat = values?.AdFloat;
            AdGbs = values?.AdGbs;
            AdGeophys = values?.AdGeophys;
            AdSeam = values?.AdSeam;
            foreach (var name in new[] { nameof(Ad), nameof(AdFloat), nameof(AdGbs), nameof(AdGeophys), nameof(AdSeam) })
                Notify(name);
        }

        private void Notify([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
