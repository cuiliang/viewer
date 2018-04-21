﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.UI;
using Viewer.UI.Attributes;
using ViewerTest.Data;

namespace ViewerTest.UI.Attributes
{
    [TestClass]
    public class AttributesPresenterTest
    {
        private IAttributeStorage _storage;
        private EntityManager _entities;
        private Mock<ISelection> _selection;
        private Mock<IAttributeView> _view;
        private Mock<IAttributeManager> _attributeManager;
        private AttributesPresenter _presenter;

        // binding of attributes in view
        private List<AttributeGroup> _viewAttributes;
        
        [TestInitialize]
        public void Setup()
        {
            var modifiedEntities = new Mock<IEntityRepository>();
            _storage = new MemoryAttributeStorage();
            _entities = new EntityManager(modifiedEntities.Object);
            _selection = new Mock<ISelection>();
            _attributeManager = new Mock<IAttributeManager>();

            // setup view mock
            _view = new Mock<IAttributeView>();
            _viewAttributes = new List<AttributeGroup>();
            _view.Setup(mock => mock.Attributes).Returns(_viewAttributes);

            var viewFactory = new ExportFactory<IAttributeView>(() =>
            {
                return new Tuple<IAttributeView, Action>(_view.Object, () => { });
            });
            _presenter = new AttributesPresenter(viewFactory, null, _selection.Object, _storage, _attributeManager.Object);
        }

        [TestMethod]
        public void AttributesChanged_DontAddAttributeIfItsNameIsEmpty()
        {
            _entities.Add(new Entity("test"));

            // add an entity to selection
            _selection.Setup(mock => mock.Items).Returns(_entities);
            _selection.Setup(mock => mock.GetEnumerator()).Returns(new[]{ 0 }.GetEnumerator() as IEnumerator<int>);
            _selection.Raise(mock => mock.Changed += null, EventArgs.Empty);

            // add a new attribute
            var oldValue = new AttributeGroup
            {
                Data = new StringAttribute("", ""),
                IsMixed = false
            };
            var newValue = new AttributeGroup
            {
                Data = new StringAttribute("", "value")
            };

            _attributeManager.Setup(mock => mock.GroupAttributesInSelection()).Returns(new AttributeGroup[] { });

            _view.Raise(mock => mock.AttributeChanged += null, new AttributeChangedEventArgs
            {
                Index = 0,
                OldValue = oldValue,
                NewValue = newValue
            });
            
            Assert.AreEqual(0, _viewAttributes.Count);
        }

        [TestMethod]
        public void AttributesChanged_AddANewAttribute()
        {
            _entities.Add(new Entity("test"));

            // add an entity to selection
            _selection.Setup(mock => mock.Items).Returns(_entities);
            _selection.Setup(mock => mock.GetEnumerator()).Returns(new[] { 0 }.GetEnumerator() as IEnumerator<int>);
            _selection.Raise(mock => mock.Changed += null, EventArgs.Empty);

            // add a new attribute
            var oldValue = new AttributeGroup
            {
                Data = new StringAttribute("",""),
                IsMixed = false
            };
            var newValue = new AttributeGroup
            {
                Data = new StringAttribute("test", "value")
            };
            _viewAttributes.Add(oldValue);
            _attributeManager.Setup(mock => mock.GroupAttributesInSelection()).Returns(new AttributeGroup[] {});

            _view.Raise(mock => mock.AttributeChanged += null, new AttributeChangedEventArgs
            {
                Index = 0,
                OldValue = oldValue,
                NewValue = newValue
            });
            
            _view.Verify(mock => mock.UpdateAttribute(0));
            _view.Verify(mock => mock.UpdateAttribute(1));

            _attributeManager.Verify(mock => mock.SetAttribute("", newValue.Data));

            Assert.AreEqual(2, _viewAttributes.Count);
            Assert.AreEqual(newValue, _viewAttributes[0]);
            Assert.AreEqual("", _viewAttributes[1].Data.Name);
        }

        [TestMethod]
        public void AttributesChanged_NonUniqueName()
        {
            var attr1 = new AttributeGroup
            {
                Data = new StringAttribute("test", "value"),
                IsMixed = false
            };
            var attr2 = new AttributeGroup
            {
                Data = new IntAttribute("test2", 42),
                IsMixed = false
            };
            var attr3 = new AttributeGroup
            {
                Data = new StringAttribute("", ""),
                IsMixed = false
            };
            _viewAttributes.Add(attr1);
            _viewAttributes.Add(attr2);
            _viewAttributes.Add(attr3);
            _attributeManager.Setup(mock => mock.GroupAttributesInSelection()).Returns(new[] { attr1, attr2 });
            
            // add attribute with an existing name
            _view.Raise(mock => mock.AttributeChanged += null, new AttributeChangedEventArgs
            {
                Index = 2,
                OldValue = _viewAttributes[2],
                NewValue = new AttributeGroup
                {
                    Data = new StringAttribute("test2", "value2"),
                    IsMixed = false
                }
            });

            _view.Verify(mock => mock.AttributeNameIsNotUnique("test2"));
            _view.Verify(mock => mock.UpdateAttribute(2));
            
            Assert.AreEqual(3, _viewAttributes.Count);
            Assert.AreEqual(attr3, _viewAttributes[2]);
        }
    }
}
