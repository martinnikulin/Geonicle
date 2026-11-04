using Microsoft.EntityFrameworkCore;

namespace Data.Models;

// Общие правила модели, действующие на все сущности. Отображение каждой сущности — в Models/Configurations.
public partial class GDB52Context
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Скаффолд ставит ClientSetNull на обязательные связи (исключения, например NoAction у границ пластопересечения,
        // заданы в конфигурации сущности). Когда строку подчинённого грида удаляют
        // из коллекции родителя (SelectedLine.LineHoles), EF считает связь разорванной и падает
        // ("association has been severed"). Каскад здесь действует только в трекере EF: схему БД и
        // её внешние ключи это не меняет, а подчинённая строка просто удаляется.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            if (foreignKey.IsRequired && foreignKey.DeleteBehavior == DeleteBehavior.ClientSetNull)
                foreignKey.DeleteBehavior = DeleteBehavior.Cascade;
        }
    }
}
