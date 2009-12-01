using System;
using Autofac.Builder;
using NUnit.Framework;
using Autofac.Core;
using System.Collections.Generic;

namespace Autofac.Tests
{
    [TestFixture]
    public class TagsFixture
    {
        class HomeController
        {
            public HomeController()
            {
            }
        }

        enum Tag { None, Outer, Middle, Inner }

        [Test]
        public void OuterSatisfiesInnerResolutions()
        {
            var builder = new ContainerBuilder();

            int instantiations = 0;

            builder.RegisterDelegate(c => { instantiations++; return ""; }).InstancePerMatchingLifetimeScope(Tag.Outer);

            var outer = builder.Build();
            outer.Tag = Tag.Outer;

            var middle = outer.BeginLifetimeScope();
            middle.Tag = Tag.Middle;

            var inner = middle.BeginLifetimeScope();
            inner.Tag = Tag.Inner;

            middle.Resolve<string>();
            outer.Resolve<string>();
            inner.Resolve<string>();

            Assert.AreEqual(1, instantiations);
        }

        [Test]
        public void AnonymousInnerContainer()
        {
            var builder = new ContainerBuilder();

            int instantiations = 0;

            builder.RegisterDelegate(c => { instantiations++; return ""; })
                .InstancePerMatchingLifetimeScope(Tag.Outer);

            var outer = builder.Build();
            outer.Tag = Tag.Outer;

            var anon = outer.BeginLifetimeScope();

            anon.Resolve<string>();
            outer.Resolve<string>();

            Assert.AreEqual(1, instantiations);
        }

        [Test]
        [ExpectedException(typeof(DependencyResolutionException))]
        public void InnerRegistrationNotAccessibleToOuter()
        {
            var builder = new ContainerBuilder();

            builder.RegisterDelegate(c => "")
                .InstancePerMatchingLifetimeScope(Tag.Middle);

            var outer = builder.Build();
            outer.Tag = Tag.Outer;

            Assert.IsTrue(outer.IsRegistered<string>());
            outer.Resolve<string>();
        }

        [Test]
        public void TaggedRegistrationsAccessibleThroughNames()
        {
            var name = "Name";

            var builder = new ContainerBuilder();

            builder.RegisterDelegate(c => "")
                .InstancePerMatchingLifetimeScope(Tag.Outer)
                .Named(name);

            var outer = builder.Build();
            outer.Tag = Tag.Outer;

            var s = (string)outer.Resolve(new NamedService(name));
            Assert.IsNotNull(s);
        }

        [Test]
        public void CorrectScopeMaintainsOwnership()
        {
            var tag = "Tag";
            var builder = new ContainerBuilder();
            builder.RegisterDelegate(c => new DisposeTracker())
                .InstancePerMatchingLifetimeScope(tag);
            var container = builder.Build();
            container.Tag = tag;
            var inner = container.BeginLifetimeScope();
            var dt = inner.Resolve<DisposeTracker>();
            Assert.IsFalse(dt.IsDisposed);
            inner.Dispose();
            Assert.IsFalse(dt.IsDisposed);
            container.Dispose();
            Assert.IsTrue(dt.IsDisposed);
        }

        [Test]
        public void DefaultSingletonSemanticsCorrect()
        {
            var tag = "Tag";
            var builder = new ContainerBuilder();
            builder.RegisterDelegate(c => new object()).InstancePerMatchingLifetimeScope(tag);
            var container = builder.Build();
            container.Tag = tag;
            var inner = container.BeginLifetimeScope();
            Assert.AreSame(container.Resolve<object>(), inner.Resolve<object>());
        }

        [Test]
        public void ReflectiveRegistration()
        {
            var tag = "Tag";
            var builder = new ContainerBuilder();
            builder.RegisterType(typeof(object)).InstancePerMatchingLifetimeScope(tag);
            var container = builder.Build();
            container.Tag = tag;
            Assert.IsNotNull(container.Resolve<object>());
        }

        [Test]
        public void CollectionsAreTaggable()
        {
            var builder = new ContainerBuilder();
            builder.RegisterCollection<object>()
                .InstancePerMatchingLifetimeScope("tag")
                .As(typeof(IList<object>));

            var outer = builder.Build();
            var inner = outer.BeginLifetimeScope();
            inner.Tag = "tag";

            var coll = inner.Resolve<IList<object>>();
            Assert.IsNotNull(coll);

            bool threw = false;
            try
            {
                outer.Resolve<IList<object>>();
            }
            catch (Exception)
            {
                threw = true;
            }

            Assert.IsTrue(threw);
        }

        [Test]
        public void GenericsAreTaggable()
        {
            var builder = new ContainerBuilder();
            builder.RegisterGeneric(typeof(List<>))
                .InstancePerMatchingLifetimeScope("tag")
                .As(typeof(IList<>));

            var outer = builder.Build();
            var inner = outer.BeginLifetimeScope();
            inner.Tag = "tag";

            var coll = inner.Resolve<IList<object>>();
            Assert.IsNotNull(coll);

            bool threw = false;
            try
            {
                outer.Resolve<IList<object>>();
            }
            catch (Exception)
            {
                threw = true;
            }

            Assert.IsTrue(threw);
        }
    }
}