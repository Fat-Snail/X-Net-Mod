using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MiniMvvm
{

    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected bool RaiseAndSetIfChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                RaisePropertyChanged(propertyName);
                return true;
            }
            return false;
        }


        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static class PropertyChangedExtensions
    {
        class PropertyObservable<T> : IObservable<T>
        {
            private readonly INotifyPropertyChanged _target;
            private readonly PropertyInfo _info;

            public PropertyObservable(INotifyPropertyChanged target, PropertyInfo info)
            {
                _target = target;
                _info = info;
            }

            class Subscription : IDisposable
            {
                private readonly INotifyPropertyChanged _target;
                private readonly PropertyInfo _info;
                private readonly IObserver<T> _observer;

                public Subscription(INotifyPropertyChanged target, PropertyInfo info, IObserver<T> observer)
                {
                    _target = target;
                    _info = info;
                    _observer = observer;
                    _target.PropertyChanged += OnPropertyChanged;
                    _observer.OnNext((T)_info.GetValue(_target));
                }

                private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName == _info.Name)
                        _observer.OnNext((T)_info.GetValue(_target));
                }

                public void Dispose()
                {
                    _target.PropertyChanged -= OnPropertyChanged;
                    _observer.OnCompleted();
                }
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                return new Subscription(_target, _info, observer);
            }
        }

        public static IObservable<TRes> WhenAnyValue<TModel, TRes>(this TModel model,
            Expression<Func<TModel, TRes>> expr) where TModel : INotifyPropertyChanged
        {
            var l = (LambdaExpression)expr;
            var ma = (MemberExpression)l.Body;
            var prop = (PropertyInfo)ma.Member;
            return new PropertyObservable<TRes>(model, prop);
        }

        public static IObservable<TRes> WhenAnyValue<TModel, T1, TRes>(this TModel model,
            Expression<Func<TModel, T1>> v1,
            Func<T1, TRes> cb
        ) where TModel : INotifyPropertyChanged
        {
            return model.WhenAnyValue(v1).Select(cb);
        }

        public static IObservable<TRes> WhenAnyValue<TModel, T1, T2, TRes>(this TModel model,
            Expression<Func<TModel, T1>> v1,
            Expression<Func<TModel, T2>> v2,
            Func<T1, T2, TRes> cb
        ) where TModel : INotifyPropertyChanged =>
            Observable.CombineLatest(
                model.WhenAnyValue(v1),
                model.WhenAnyValue(v2),
                cb);

        public static IObservable<ValueTuple<T1, T2>> WhenAnyValue<TModel, T1, T2>(this TModel model,
            Expression<Func<TModel, T1>> v1,
            Expression<Func<TModel, T2>> v2
        ) where TModel : INotifyPropertyChanged =>
            model.WhenAnyValue(v1, v2, (a1, a2) => (a1, a2));

        public static IObservable<TRes> WhenAnyValue<TModel, T1, T2, T3, TRes>(this TModel model,
            Expression<Func<TModel, T1>> v1,
            Expression<Func<TModel, T2>> v2,
            Expression<Func<TModel, T3>> v3,
            Func<T1, T2, T3, TRes> cb
        ) where TModel : INotifyPropertyChanged =>
            Observable.CombineLatest(
                model.WhenAnyValue(v1),
                model.WhenAnyValue(v2),
                model.WhenAnyValue(v3),
                cb);

        public static IObservable<ValueTuple<T1, T2, T3>> WhenAnyValue<TModel, T1, T2, T3>(this TModel model,
            Expression<Func<TModel, T1>> v1,
            Expression<Func<TModel, T2>> v2,
            Expression<Func<TModel, T3>> v3
        ) where TModel : INotifyPropertyChanged =>
            model.WhenAnyValue(v1, v2, v3, (a1, a2, a3) => (a1, a2, a3));
    }


    public sealed class MiniCommand<T> : MiniCommand, ICommand
    {
        private readonly Action<T> _cb;
        private bool _busy;
        private Func<T, Task> _acb;

        public MiniCommand(Action<T> cb)
        {
            _cb = cb;
        }

        public MiniCommand(Func<T, Task> cb)
        {
            _acb = cb;
        }

        private bool Busy
        {
            get => _busy;
            set
            {
                _busy = value;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }


        public override event EventHandler CanExecuteChanged;
        public override bool CanExecute(object parameter) => !_busy;

        public override async void Execute(object parameter)
        {
            if (Busy)
                return;
            try
            {
                Busy = true;
                if (_cb != null)
                    _cb((T)parameter);
                else
                    await _acb((T)parameter);
            }
            finally
            {
                Busy = false;
            }
        }
    }

    public abstract class MiniCommand : ICommand
    {
        public static MiniCommand Create(Action cb) => new MiniCommand<object>(_ => cb());
        public static MiniCommand Create<TArg>(Action<TArg> cb) => new MiniCommand<TArg>(cb);
        public static MiniCommand CreateFromTask(Func<Task> cb) => new MiniCommand<object>(_ => cb());

        public abstract bool CanExecute(object parameter);
        public abstract void Execute(object parameter);
        public abstract event EventHandler CanExecuteChanged;
    }
}