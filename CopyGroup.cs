using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlugin
{
    [TransactionAttribute(TransactionMode.Manual)] // Manual - ручной режим
    public class CopyGroup : IExternalCommand

    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try // добавляем обработку исключений
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument; //  - дполучили доступ к документу 
                Document doc = uiDoc.Document; // рлдучаем ссылку на экз-р классса Document, кт будкт содержать базу данных эл-тов внутри окрытого эл-та

                GroupPickFilter groupPickFilter = new GroupPickFilter();
                //попросить пользователя выбрать группу для копирования
                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберите группу объектов"); // получили ссылку на выбранную пользователем группу объектов
                Element element = doc.GetElement(reference);
                Group group = element as Group; // тот объект по кт пользователь щелкнул получили и преобразовали к типу Group
                XYZ groupCenter = GetElementCenter(group);  // находим цетр группы
                Room room = GetRoomByPoint(doc, groupCenter); // в какую комнату попадает точка
                XYZ roomCenter = GetElementCenter(room); // находим центр комнаты
                XYZ offset = groupCenter - roomCenter; // определить смещение центра группы относительно центра комнаты


                //попросим пользователя выбрать какую то точку 
                XYZ point = uiDoc.Selection.PickPoint("Выберите точку");
                Room room2 = GetRoomByPoint(doc, point); // определяем комнату по кт щелкнул пользователь
                XYZ room2Center = GetElementCenter(room2); // найти центр этой комнаты
                XYZ offset2 = room2Center + offset; // на основе смещения вычисляем точку в кт необходимо выполнить вставку группы

                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы объектов");
                doc.Create.PlaceGroup(point, group.GroupType);
                transaction.Commit();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException) // обработка исключений (отдельно нажатие ESC)
            {
                return Result.Cancelled;
            }
            catch (Exception ex) // обработка исключений (отдельно все остальные ошибки кт могли произойти в процессе работы)
            {
                message = ex.Message; // выводим сообщение об ошибке 
                return Result.Failed;
            }
            return Result.Succeeded;
        }

        public XYZ GetElementCenter(Element element) // метод кт по объекту по эл-ту вычисляет его центр на основе BoundingBox
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null); // рамка в 3х измерениях
            return (bounding.Max + bounding.Min) / 2;
        }

        public Room GetRoomByPoint(Document doc, XYZ point) // метод должен определять комнату поисходной точке
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            foreach (Element e in collector)
            {
                Room room = e as Room;
                if (room != null)
                {
                    if (room.IsPointInRoom(point))
                    {
                        return room;
                    }
                }
            }
            return null;
        }
    }

    public class GroupPickFilter : ISelectionFilter //создаем класс для фильтра
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups) // если эл-т группа - true
                return true;
            else  // если эл-т что то дргугое  - false
                return false; 
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
