using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ConfOrm;
using ConfOrm.NH;
using ConfOrm.Patterns;
using ConfOrm.Shop.Appliers;
using ConfOrm.Shop.CoolNaming;
using ConfOrm.Shop.Packs;
using Mystic;
using NHibernate;
using NHibernate.ByteCode.Castle;
using NHibernate.Cfg;
using NHibernate.Cfg.Loquacious;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Tool.hbm2ddl;

namespace NHibernateMystic
{
	public class NHibernateInitializer
	{
		private const string ConnectionString =
			@"Data Source=localhost\SQLEXPRESS;Initial Catalog=IntroNH;Integrated Security=True;Pooling=False";

		private Configuration configure;
		private ISessionFactory sessionFactory;

		private readonly Type[] baseTypesToRecognizeRootEntities = new[] { typeof(AbstractEntity<>), typeof(Entity) };
		private readonly Type[] tablePerClassHierarchy = new Type[] { };
		private readonly Type[] tablePerConcreteClass = new Type[] { };

		#region NH Startup

		public ISessionFactory SessionFactory
		{
			get { return sessionFactory ?? (sessionFactory = configure.BuildSessionFactory()); }
		}

		public void Initialize()
		{
			configure = new Configuration();
			configure.SessionFactoryName("Demo");
			configure.Proxy(p =>
			                {
			                	p.ProxyFactoryFactory<ProxyFactoryFactory>();
			                });
			configure.DataBaseIntegration(db =>
			                              {
			                              	db.Dialect<MsSql2008Dialect>();
			                              	db.Driver<SqlClientDriver>();
			                              	db.KeywordsAutoImport = Hbm2DDLKeyWords.AutoQuote;
			                              	db.IsolationLevel = IsolationLevel.ReadCommitted;
			                              	db.ConnectionString = ConnectionString;
			                              	db.BatchSize = 20;
			                              	db.Timeout = 10;
			                              	db.HqlToSqlSubstitutions = "true 1, false 0, yes 'Y', no 'N'";
			                              });
			Map();
		}

		public void CreateSchema()
		{
			new SchemaExport(configure).Create(false, true);
		}

		public void DropSchema()
		{
			new SchemaExport(configure).Drop(false, true);
		}

		private void Map()
		{
			configure.AddDeserializedMapping(GetMapping(), "MysticDomain");
		}

		#endregion

		#region Utils

		private bool IsRootEntity(Type type)
		{
			Type baseType = type.BaseType;
			return baseType != null && !IsOnlyBaseTypesToRecognizeRootEntities(type) &&
			       (baseTypesToRecognizeRootEntities.Contains(baseType) || (baseType.IsGenericType && baseTypesToRecognizeRootEntities.Contains(baseType.GetGenericTypeDefinition())));
		}

		private bool IsOnlyBaseTypesToRecognizeRootEntities(Type type)
		{
			return (baseTypesToRecognizeRootEntities.Contains(type) || (type.IsGenericType && baseTypesToRecognizeRootEntities.Contains(type.GetGenericTypeDefinition())));
		}

		#endregion

		public HbmMapping GetMapping()
		{
			#region Initialize ConfORM

			var orm = new ObjectRelationalMapper();
			IPatternsAppliersHolder patternsAppliers =
				(new SafePropertyAccessorPack())
					.Merge(new OneToOneRelationPack(orm))
					.Merge(new BidirectionalManyToManyRelationPack(orm))
					.Merge(new BidirectionalOneToManyRelationPack(orm))
					.Merge(new DiscriminatorValueAsClassNamePack(orm))
					.Merge(new CoolTablesNamingPack(orm))
					.Merge(new CoolColumnsNamingPack(orm))
					.Merge(new TablePerClassPack())
					.Merge(new UseNoLazyForNoProxablePack()) // <== Lazy false when the class is not proxable
					.Merge(new ConfOrm.Shop.CoolNaming.UnidirectionalOneToManyMultipleCollectionsKeyColumnApplier(orm))
					.Merge(new UseCurrencyForDecimalApplier())
					.Merge(new DatePropertyByNameApplier())
					.Merge(new MsSQL2008DateTimeApplier());

			orm.Patterns.PoidStrategies.Add(new HighLowPoidPattern(new {max_lo = 100}));

			var mapper = new Mapper(orm, patternsAppliers);

			var domainEntities = typeof (Entity)
				.Assembly.GetTypes()
				.Where(t => (typeof (AbstractEntity<int>).IsAssignableFrom(t) || typeof (AbstractEntity<Guid>).IsAssignableFrom(t)) 
					&& !t.IsGenericType)
				.ToList();

			IEnumerable<Type> tablePerClassEntities = typeof (Entity)
				.Assembly.GetTypes().Where(t => IsRootEntity(t)
				                                && !tablePerClassHierarchy.Contains(t)
				                                && !tablePerConcreteClass.Contains(t)).ToList();

			// Mappings
			orm.TablePerClass(tablePerClassEntities);
			orm.TablePerClassHierarchy(tablePerClassHierarchy);
			orm.TablePerConcreteClass(tablePerConcreteClass);

			#endregion

			ConfOrmMapping(orm, mapper);

			return mapper.CompileMappingFor(domainEntities);
		}


		private void ConfOrmMapping(ObjectRelationalMapper orm, Mapper mapper)
		{
		}
	}
}