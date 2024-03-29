﻿using Azure.Identity;
using CourseProject2022FallBL.Models;
using CourseProject2022FallBL.Services;
using CourseProject2022FallWPF.Model.Commands;
using CourseProject2022FallWPF.Services;
using LiveCharts;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using System.Windows.Data;
using System.Windows.Input;
using Action = CourseProject2022FallBL.Models.Action;

namespace CourseProject2022FallWPF.ViewModel
{
    public class FormattedViewViewModel : ViewModel
    {
        private readonly DialogVisitor Visitor = new();
        private static string DeleteErrorMessage(string name) => $"An error occurred when deleting a {name}";

        public FormattedViewViewModel()
        {
            TypeSelected = new LambdaCommand(OnTypeSelected, CanTypeSelected);
            AddItem = new LambdaCommand(OnAddItem, CanAddItem);
            EditItem = new LambdaCommand(OnEditItem, CanEditItem);
            RemoveItem = new LambdaCommand(OnRemoveItem, CanRemoveItem);
            Reload = new LambdaCommand(OnReload, CanReload);
            OnTypeSelected("All");
            ItemsView.Filter = ItemsFilter;
        }

        #region ObservableItems
        private ObservableCollection<Action> _ObservableItems = new (DataService.GetActions());
        public ObservableCollection<Action> ObservableItems
        {
            get => _ObservableItems;
            set => Set(ref _ObservableItems, value);
        }

        public ICollectionView ItemsView { get => CollectionViewSource.GetDefaultView(_ObservableItems); }
        private bool ItemsFilter(object item)
        {
            if (item is Income && IsIncomeSelected)
            {
                return true;
            }
            else if (item is Expense && IsExpenseSelected)
            {
                return true;
            }
            else if (IsAllSelected)
            {
                return true;
            }
            return false;
        }
        #endregion

        #region IsIncomeSelected
        private bool _IsIncomeSelected = false;
        public bool IsIncomeSelected
        {
            get => _IsIncomeSelected;
            set => Set(ref _IsIncomeSelected, value);
        }
        #endregion

        #region IsExpenseSelected
        private bool _IsExpenseSelected = false;
        public bool IsExpenseSelected
        {
            get => _IsExpenseSelected;
            set => Set(ref _IsExpenseSelected, value);
        }
        #endregion

        #region IsAllSelected
        private bool _IsAllSelected = true;
        public bool IsAllSelected
        {
            get => _IsAllSelected;
            set => Set(ref _IsAllSelected, value);
        }
        #endregion

        #region TypeSelected

        public ICommand TypeSelected { get; }

        private bool CanTypeSelected(object p) => true;
        private void OnTypeSelected(object p)
        {
            if (p is string str)
            {
                switch (str)
                {
                    case "All":
                        if (!IsAllSelected)
                            ChangeState(true, false, false);
                        break;
                    case "Income":
                        if (!IsIncomeSelected)
                            ChangeState(false, true, false);
                        break;
                    case "Expense":
                        if (!IsExpenseSelected)
                            ChangeState(false, false, true);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ChangeState(bool all, bool income, bool expense)
        {
            IsAllSelected = all;
            IsIncomeSelected = income;
            IsExpenseSelected = expense;
            ItemsView.Refresh();
        }

        #endregion

        #region Reload

        public ICommand Reload { get; }

        private bool CanReload(object p) => true;
        private void OnReload(object p)
        {
            ObservableItems = new(DataService.GetActions());
        }

        #endregion

        #region AddItem

        public ICommand AddItem { get; }

        private bool CanAddItem(object p) => true;
        private void OnAddItem(object p)
        {
            var action = (Action?)Visitor.DynamicVisit(new Action());
            if (action == null)
                return;
            if (action is Income inc && inc.isDefault)
                return;
            if (action is Expense exp && exp.isDefault)
                return;

            // TODO: Refactor
            if (action is Income income)
            {
                DataService.UpsertOperation(income.Operation);
                income.Operation.User.ID = DataService.GetUserID(income.Operation.User);
                income.Operation.Target.ID = DataService.GetTargetID(income.Operation.Target);
                income.Operation.Currency.ID = DataService.GetCurrencyID(income.Operation.Currency);
                income.Operation.ID = DataService.GetOperationID(income.Operation);
                DataService.UpsertIncome(income);
                income.ID = DataService.GetIncomeID(income);
                if (ObservableItems.Where(a => a is Income && a.ID == income.ID).IsNullOrEmpty())
                    ObservableItems.Add(income);
                else
                    foreach (var a in ObservableItems.Where(a => a is Income && a.ID == income.ID))
                    {
                        a.Operation.Value = income.Operation.Value;
                        a.Operation.Comment = income.Operation.Comment;
                        a.Operation.Target = income.Operation.Target;
                        a.Operation.User = income.Operation.User;
                        a.Operation.Currency = income.Operation.Currency;
                    }
            }
            else if (action is Expense expense)
            {
                DataService.UpsertOperation(expense.Operation);
                expense.Operation.User.ID = DataService.GetUserID(expense.Operation.User);
                expense.Operation.Target.ID = DataService.GetTargetID(expense.Operation.Target);
                expense.Operation.Currency.ID = DataService.GetCurrencyID(expense.Operation.Currency);
                expense.Operation.ID = DataService.GetOperationID(expense.Operation);
                DataService.UpsertExpense(expense);
                expense.ID = DataService.GetExpenseID(expense);
                if (ObservableItems.Where(a => a is Expense && a.ID == expense.ID).IsNullOrEmpty())
                    ObservableItems.Add(expense);
                else
                    foreach (var a in ObservableItems.Where(a => a is Expense && a.ID == expense.ID))
                    {
                        a.Operation.Value = expense.Operation.Value;
                        a.Operation.Comment = expense.Operation.Comment;
                        a.Operation.Target = expense.Operation.Target;
                        a.Operation.User = expense.Operation.User;
                        a.Operation.Currency = expense.Operation.Currency;
                    }
            }
        }

        #endregion

        #region EditItem

        public ICommand EditItem { get; }

        private bool CanEditItem(object p) => true;
        private void OnEditItem(object p)
        {
            var action = p as Action;
            action = (Action?)Visitor.DynamicVisit(action);
            if (action == null)
                return;
            if (action is Income inc && inc.isDefault)
                return;
            if (action is Expense exp && exp.isDefault)
                return;

            // TODO: Refactor
            if (action is Income income)
            {
                DataService.UpdateIncome(income);
                foreach (var a in ObservableItems.Where(a => a is Income && a.ID == income.ID))
                {
                    a.Operation.Value = income.Operation.Value;
                    a.Operation.Comment = income.Operation.Comment;
                    a.Operation.Target = income.Operation.Target;
                    a.Operation.User = income.Operation.User;
                    a.Operation.Currency = income.Operation.Currency;
                }
            }
            else if (action is Expense expense)
            {
                DataService.UpdateExpense(expense);
                foreach (var a in ObservableItems.Where(a => a is Expense && a.ID == expense.ID))
                {
                    a.Operation.Value = expense.Operation.Value;
                    a.Operation.Comment = expense.Operation.Comment;
                    a.Operation.Target = expense.Operation.Target;
                    a.Operation.User = expense.Operation.User;
                    a.Operation.Currency = expense.Operation.Currency;
                }
            }
        }

        #endregion

        #region RemoveItem

        public ICommand RemoveItem { get; }

        private bool CanRemoveItem(object p) => true;
        private void OnRemoveItem(object p)
        {
            var action = p as Action;
            if (action is Income income)
            {
                if (DataService.RemoveIncome(income))
                    ObservableItems.Remove(income);
                else
                    Visitor.DynamicVisit(DeleteErrorMessage("income"));
            }
            else if (action is Expense expense)
            {
                if (DataService.RemoveExpense(expense))
                    ObservableItems.Remove(expense);
                else
                    Visitor.DynamicVisit(DeleteErrorMessage("expense"));

            }
        }

        #endregion
    }
}
