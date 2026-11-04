using System.ComponentModel.DataAnnotations;

namespace Data.Models;

/// <summary>
/// Значение поля «КБ» пластопересечения. В БД хранится строкой (до 12 символов): подпись из [Display] или, если её нет,
/// имя члена (см. Infrastructure/DisplayNameEnumConverter). Подписи в гриде берутся оттуда же.
/// </summary>
public enum KbType
{
    Б,

    [Display(Name = "Б(без керна)")]
    БезКерна,

    К,

    [Display(Name = "К + Б")]
    КБ,

    [Display(Name = "Б(К)")]
    БК,

    [Display(Name = "Б(НК)")]
    БНК,

    [Display(Name = "Нет данных")]
    НетДанных,
}

