using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using NewLife.Reflection;

namespace MiniMvvm;

public class SimpleMapper
{
    private static System.Collections.Concurrent.ConcurrentDictionary<string, MapperConfiguration> _configurations =
        new ConcurrentDictionary<string, MapperConfiguration>();

    public static void Config<TDestination>(Action<BindingConfigOf<TDestination>> confAct)
        where TDestination : new()
    {

        var bindingConfig = new BindingConfigOf<TDestination>();
        confAct.Invoke(bindingConfig);

        var config = bindingConfig.BuildConfig();

        var tf = typeof(TDestination);
        _configurations[tf.FullName] = config;

    }

    public static TDestination Map<TDestination>(object source)
        where TDestination : new()
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source), "Source object is null");
        }

        
        var destination = new TDestination();

        var ignoreProps = new string[0];
        if (_configurations.TryGetValue(destination.GetType().FullName, out var config))
        {
            ignoreProps = config.IgnoreProps;
        }
        
        if(ignoreProps?.Length>0)
            destination.Copy(source,false,ignoreProps.ToArray());
        else
            destination.Copy(source);

        if (config.PropLinks?.Count > 0)
        {
            foreach (var link in config.PropLinks)
            {
               var value= source.GetValue(link.SourceName);
               destination.SetValue(link.DesName, value);
            }
        }

        return destination;
    }

    internal class MapperConfiguration
    {
        public string[] IgnoreProps { get; private set; }
        public List<PropertyLink> PropLinks { get; private set; } 

        public MapperConfiguration(List<string> ignoreProps,List<PropertyLink> propLinks)
        {
            if (ignoreProps?.Count > 0)
                this.IgnoreProps = ignoreProps.ToArray();
            this.PropLinks = propLinks;
        }

    }
    
    public class BindingConfigOf<TDestination>
    {
        public List<string> IgnoreProps { get; } = new List<string>();
        internal List<PropertyLink> PropLinks { get; } = new List<PropertyLink>();

        public void Ignore(Expression<Func<TDestination, object>> property)
        {
            var name = GetProperName(property.Body);
            this.IgnoreProps.Add(name);
        }

        private string GetProperName(Expression expressionBody)
        {
            if (expressionBody is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }
            else if (expressionBody is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression)
            {
                return ((MemberExpression)unaryExpression.Operand).Member.Name;
            }
            else
            {
                throw new ArgumentException("The expression is not a member access expression.");
            }
        }

        public void Bind<TSource>(Expression<Func<TDestination, object>> destProp,
            Expression<Func<TSource, object>> sourceProp)
        {
            if(destProp==null||sourceProp==null)
                return;

            var destName = GetProperName(destProp.Body);
            var sourceName = GetProperName(sourceProp.Body);

            PropLinks.Add(new PropertyLink { DesName = destName, SourceName = sourceName });
        }

        internal MapperConfiguration BuildConfig()
        {
            return new MapperConfiguration(IgnoreProps,PropLinks);
        }
    }
    internal class PropertyLink
    {
        internal string DesName { get; set; }
        internal string SourceName { get; set; }
    }

    
}