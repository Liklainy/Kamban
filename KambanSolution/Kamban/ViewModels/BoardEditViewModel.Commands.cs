﻿using DynamicData;
using Kamban.MatrixControl;
using Kamban.Model;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kamban.ViewModels
{
    public partial class BoardEditViewModel
    {
        private async Task HeadRenameCommandExecute(IDim head)
        {
            var headTxt = head is ColumnViewModel ? "column" : "row";
            headTxt += $" {head.Name}";

            var ts = await dialCoord
                .ShowInputAsync(this, "Warning", $"Enter new name for {headTxt}",
                    new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "OK",
                        NegativeButtonText = "Cancel",
                        DefaultText = head.Name
                    });

            if (string.IsNullOrEmpty(ts))
                return;

            var column = head as ColumnViewModel;
            var row = head as RowViewModel;

            if (column!=null)
                column.Name = ts;
            else
                row.Name = ts;
        }

        private async Task HeadDeleteCommandExecute(IDim head)
        {
            var column = head as ColumnViewModel;
            var row = head as RowViewModel;

            var headTxt = head is ColumnViewModel ? "column" : "row";
            headTxt += $" '{head.Name}'";

            if (column != null && Columns.Count <= 1) return;
            if (row != null && Rows.Count <= 1) return;

            var ts = await dialCoord.ShowMessageAsync(this, "Warning",
                $"Are you shure to delete {headTxt}?"
                , MessageDialogStyle.AffirmativeAndNegative);

            if (ts == MessageDialogResult.Negative)
                return;

            this.EnableMatrix = false;

            // delete head and move cards from deleted cells to first head
            if (column != null)
            {
                // Shift cards
                var firstColumn = Columns.OrderBy(x => x.Order).First();
                foreach (CardViewModel it in Cards.Items.Where(x => (int)x.ColumnDeterminant == column.Id))
                    it.ColumnDeterminant = firstColumn.Determinant;

                Db.Columns.Remove(column);
            }
            else
            {
                // Shift cards
                var firstRow = Rows.OrderBy(x => x.Order).First();
                foreach (CardViewModel it in Cards.Items.Where(x => (int)x.RowDeterminant == row.Id))
                    it.RowDeterminant = firstRow.Determinant;

                Db.Rows.Remove(row);
            }

            // Rebuild Matrix
            this.EnableMatrix = true;

            NormalizeGridCommand
                .Execute()
                .Subscribe();
        }

        private async Task InsertHeadBeforeCommandExecute(IDim head)
        {
            await InsertHead(head, 0);
        }

        private async Task InsertHeadAfterCommandExecute(IDim head)
        {
            await InsertHead(head, 1);
        }

        private async Task InsertHead(IDim head, int after)
        {
            var ts = await dialCoord
                .ShowInputAsync(this, "Info", $"Enter new name",
                    new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "OK",
                        NegativeButtonText = "Cancel",
                        DefaultText = head.Name
                    });

            if (string.IsNullOrEmpty(ts))
                return;

            var column = head as ColumnViewModel;
            var row = head as RowViewModel;

            this.EnableMatrix = false;

            if (column != null)
            {
                var tempColumns = Columns.ToList();
                var indx = tempColumns.IndexOf(head) + after;

                var cvm = new ColumnViewModel
                {
                    Name = ts,
                    BoardId = column.BoardId
                };

                tempColumns.Insert(indx, cvm);
                Db.Columns.Add(cvm);

                int i = 0;
                foreach (var it in tempColumns)
                {
                    it.Order = i;
                    i++;
                }
            }
            else
            {
                var tempRows = Rows.ToList();
                var indx = tempRows.IndexOf(head) + after;

                var rvm = new RowViewModel
                {
                    Name = ts,
                    BoardId = row.BoardId
                };

                tempRows.Insert(indx, rvm);
                Db.Rows.Add(rvm);

                int i = 0;
                foreach (var it in tempRows)
                {
                    it.Order = i;
                    i++;
                }
            }

            // Rebuild matrix
            this.EnableMatrix = true;

            NormalizeGridCommand
                .Execute()
                .Subscribe();
        }

        private async Task RenameBoardCommandExecute()
        {
            var oldName = CurrentBoard.Name;
            var str = $"Enter new board name for \"{oldName}\"";
            var newName = await dialCoord
            .ShowInputAsync(this, "Board rename", str,
                new MetroDialogSettings()
                {
                    AffirmativeButtonText = "OK",
                    NegativeButtonText = "Cancel",
                    DefaultText = CurrentBoard?.Name
                });

            if (string.IsNullOrEmpty(newName))
                return;

            CurrentBoard.Name = newName;
            //var bi = mapper.Map<BoardViewModel, BoardInfo>(CurrentBoard);
            //prjService.CreateOrUpdateBoardAsync(bi);
            Title = newName;
        }

        private async Task DeleteBoardCommandExecute()
        {
            var ts = await dialCoord.ShowMessageAsync(this, "Warning",
                $"Are you shure to delete board '{CurrentBoard.Name}'?"
                , MessageDialogStyle.AffirmativeAndNegative);

            if (ts == MessageDialogResult.Negative)
                return;

            // protect
            if (Db.Boards.Count <= 1)
                return;

            // TODO: use global cards

            // Remove cards
            foreach (var card in Cards.Items.ToList())
                prjService.DeleteIssueAsync(card.Id);

            Cards.Clear();

            // Remove headers
            Db.Columns.Items
                .Where(x => x.BoardId == CurrentBoard.Id)
                .ToList()
                .ForEach(x => Db.Columns.Remove(x));

            Db.Rows.Items
                .Where(x => x.BoardId == CurrentBoard.Id)
                .ToList()
                .ForEach(x => Db.Rows.Remove(x));

            // Remove board
            Db.Boards.Remove(CurrentBoard);
            CurrentBoard = Db.Boards.Items.First();
        }

        private async Task DeleteCardCommandExecuteAsync(ICard cvm)
        {
            var ts = await dialCoord.ShowMessageAsync(this, "Warning",
                $"Are you shure to delete issue '{cvm.Header}'?"
                , MessageDialogStyle.AffirmativeAndNegative);

            if (ts == MessageDialogResult.Negative)
                return;

            // TODO: use global cards

            prjService.DeleteIssueAsync(cvm.Id);
            Cards.Remove(cvm);
        }

    }//end of class
}
