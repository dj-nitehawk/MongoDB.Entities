using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Tests.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDB.Entities.Tests;

[TestClass]
public class ObjectIdRelationships
{
    [TestMethod]
    public async Task setting_one_to_one_reference_returns_correct_entity()
    {
        var model = new CarModel { Name = "F-150" };
        var make = new CarMake { Name = "sotorrce" };
        await make.SaveAsync();
        model.Make = make.ToReference();
        await model.SaveAsync();
        var res = await (await model.Queryable()
                      .Where(b => b.Id == model.Id)
                      .SingleAsync())
                      .Make.ToEntityAsync();
        Assert.AreEqual(make.Name, res!.Name);
    }

    [TestMethod]
    public async Task setting_one_to_one_reference_with_implicit_operator_by_string_id_returns_correct_entity()
    {
        var model = new CarModel { Name = "Camaro" };
        var make = new CarMake { Name = "sotorrce" };
        await make.SaveAsync();
        model.Make = make.ToReference();
        await model.SaveAsync();
        var res = await (await model.Queryable()
                      .Where(b => b.Id == model.Id)
                      .SingleAsync())
                      .Make.ToEntityAsync();
        Assert.AreEqual(make.Name, res!.Name);
    }

    [TestMethod]
    public async Task setting_one_to_one_reference_with_implicit_operator_by_entity_returns_correct_entity()
    {
        var model = new CarModel { Name = "911" };
        var make = new CarMake { Name = "soorfioberce" };
        await make.SaveAsync();
        model.Make = make.ToReference();
        await model.SaveAsync();
        var res = await (await model.Queryable()
                      .Where(b => b.Id == model.Id)
                      .SingleAsync())
                      .Make.ToEntityAsync();
        Assert.AreEqual(make.Name, res!.Name);
    }

    [TestMethod]
    public async Task one_to_one_to_entity_with_lambda_projection()
    {
        var model = new CarModel { Name = "model" };
        var make = new CarMake { Name = "ototoewlp" };
        await make.SaveAsync();
        model.Make = make.ToReference();
        await model.SaveAsync();
        var res = await (await model.Queryable()
                      .Where(b => b.Id == model.Id)
                      .SingleAsync())
                      .Make.ToEntityAsync(a => new CarMake { Name = a.Name });
        Assert.AreEqual(make.Name, res!.Name);
        Assert.AreEqual(null, res.Id);
    }

    [TestMethod]
    public async Task one_to_one_to_entity_with_mongo_projection()
    {
        var model = new CarModel { Name = "model" };
        var make = new CarMake { Name = "ototoewmp" };
        await make.SaveAsync();
        model.Make = make.ToReference();
        await model.SaveAsync();
        var res = await (await model.Queryable()
                      .Where(b => b.Id == model.Id)
                      .SingleAsync())
                      .Make.ToEntityAsync(p => p.Include(a => a.Name).Exclude(a => a.Id));
        Assert.AreEqual(make.Name, res!.Name);
        Assert.AreEqual(null, res.Id);
    }

    [TestMethod]
    public async Task adding_one2many_references_returns_correct_entities_queryable()
    {
        var make = new CarMake { Name = "make" };
        var model1 = new CarModel { Name = "aotmrrceb1" };
        var model2 = new CarModel { Name = "aotmrrceb2" };
        await model1.SaveAsync(); await model2.SaveAsync();
        await make.SaveAsync();
        await make.Models.AddAsync(model1);
        await make.Models.AddAsync(model2);
        var models = await make.Queryable()
                          .Where(a => a.Id == make.Id)
                          .Single()
                          .Models
                          .ChildrenQueryable().ToListAsync();
        Assert.AreEqual(model2.Name, models[1].Name);
    }

    [TestMethod]
    public async Task ienumerable_for_many()
    {
        var make = new CarMake { Name = "make" };
        var model1 = new CarModel { Name = "aotmrrceb1" };
        var model2 = new CarModel { Name = "aotmrrceb2" };
        await model1.SaveAsync(); await model2.SaveAsync();
        await make.SaveAsync();
        await make.Models.AddAsync(model1);
        await make.Models.AddAsync(model2);
        var models = (await make.Queryable()
                          .Where(a => a.Id == make.Id)
                          .SingleAsync())
                          .Models;

        List<CarModel> modellist = new();

        foreach (var model in models)
        {
            modellist.Add(model);
        }

        Assert.AreEqual(2, modellist.Count);
    }

    [TestMethod]
    public async Task adding_one2many_references_returns_correct_entities_fluent()
    {
        var make = new CarMake { Name = "make" };
        var model1 = new CarModel { Name = "aotmrrcebf1" };
        var model2 = new CarModel { Name = "aotmrrcebf2" };
        await model1.SaveAsync(); await model2.SaveAsync();
        await make.SaveAsync();
        await make.Models.AddAsync(model1);
        await make.Models.AddAsync(model2);
        var models = await make.Queryable()
                          .Where(a => a.Id == make.Id)
                          .Single()
                          .Models
                          .ChildrenFluent().ToListAsync();
        Assert.AreEqual(model2.Name, models[1].Name);
    }

    [TestMethod]
    public async Task many_children_count()
    {
        var model1 = new CarModel { Name = "mcc" }; await model1.SaveAsync();
        var color1 = new CarColor { Name = "ac2mrceg1" }; await color1.SaveAsync();
        var color2 = new CarColor { Name = "ac2mrceg1" }; await color2.SaveAsync();

        await model1.Colors.AddAsync(color1);
        await model1.Colors.AddAsync(color2);

        Assert.AreEqual(2, await model1.Colors.ChildrenCountAsync());

        var model2 = new CarModel { Name = "mcc" }; await model2.SaveAsync();

        await color1.Models.AddAsync(model1);
        await color1.Models.AddAsync(model2);

        Assert.AreEqual(2, await color1.Models.ChildrenCountAsync());
    }

    [TestMethod]
    public async Task adding_many2many_returns_correct_children()
    {
        var model1 = new CarModel { Name = "ac2mrceb1" }; await model1.SaveAsync();
        var model2 = new CarModel { Name = "ac2mrceb2" }; await model2.SaveAsync();

        var color1 = new CarColor { Name = "ac2mrceg1" }; await color1.SaveAsync();
        var color2 = new CarColor { Name = "ac2mrceg1" }; await color2.SaveAsync();

        await model1.Colors.AddAsync(color1);
        await model1.Colors.AddAsync(color2);
        await model1.Colors.AddAsync(color1);
        Assert.AreEqual(2, DB.Queryable<CarModel>().Where(b => b.Id == model1.Id).Single().Colors.ChildrenQueryable().Count());
        Assert.AreEqual(color1.Name, model1.Colors.ChildrenQueryable().First().Name);

        await color1.Models.AddAsync(model1);
        await color1.Models.AddAsync(model2);
        await color1.Models.AddAsync(model1);
        Assert.AreEqual(2, color1.Queryable().Where(g => g.Id == color1.Id).Single().Models.ChildrenQueryable().Count());
        Assert.AreEqual(color1.Name, model2.Queryable().Where(b => b.Id == model2.Id).Single().Colors.ChildrenQueryable().First().Name);

        await color2.Models.AddAsync(model1);
        await color2.Models.AddAsync(model2);
        Assert.AreEqual(2, model1.Colors.ChildrenQueryable().Count());
        Assert.AreEqual(color2.Name, model2.Queryable().Where(b => b.Id == model2.Id).Single().Colors.ChildrenQueryable().First().Name);
    }

    [TestMethod]
    public async Task removing_many2many_returns_correct_children()
    {
        var model1 = new CarModel { Name = "rm2mrceb1" }; await model1.SaveAsync();
        var model2 = new CarModel { Name = "rm2mrceb2" }; await model2.SaveAsync();

        var color1 = new CarColor { Name = "rm2mrceg1" }; await color1.SaveAsync();
        var color2 = new CarColor { Name = "rm2mrceg1" }; await color2.SaveAsync();

        await model1.Colors.AddAsync(color1);
        await model1.Colors.AddAsync(color2);
        await model2.Colors.AddAsync(color1);
        await model2.Colors.AddAsync(color2);

        await model1.Colors.RemoveAsync(color1);
        Assert.AreEqual(1, await model1.Colors.ChildrenQueryable().CountAsync());
        Assert.AreEqual(color2.Name, (await model1.Colors.ChildrenQueryable().SingleAsync()).Name);
        Assert.AreEqual(1, await color1.Models.ChildrenQueryable().CountAsync());
        Assert.AreEqual(model2.Name, (await color1.Models.ChildrenQueryable().FirstAsync()).Name);
    }

    [TestMethod]
    public async Task getting_parents_of_a_relationship_fluent_works()
    {
        var guid = Guid.NewGuid().ToString();

        var model = new CarModel { Name = "Corvette " + guid };
        await model.SaveAsync();

        var color = new CarColor { Name = "Midnight Black " + guid };
        await color.SaveAsync();

        var color1 = new CarColor { Name = "Candy Apple Red " + guid };
        await color1.SaveAsync();

        await model.Colors.AddAsync(color);
        await model.Colors.AddAsync(color1);

        var models = await model.Colors
                        .ParentsFluent(color.Id)
                        .ToListAsync();

        Assert.AreEqual(1, models.Count);
        Assert.AreEqual(model.Name, models.Single().Name);

        models = await model.Colors
                        .ParentsFluent(color.Fluent().Match(g => g.Name.Contains(guid)))
                        .ToListAsync();

        Assert.AreEqual(1, models.Count);
        Assert.AreEqual(model.Name, models.Single().Name);

        var colors = await color.Models
                          .ParentsFluent(new object[] { model.Id! })
                          .ToListAsync();

        Assert.AreEqual(2, colors.Count);
        Assert.AreEqual(color.Name, colors.Single(g => g.Id == color.Id).Name);

        colors = await color.Models
                .ParentsFluent(model.Fluent().Match(b => b.Id == model.Id))
                .ToListAsync();

        Assert.AreEqual(1, models.Count);
        Assert.AreEqual(model.Name, models.Single().Name);
    }

    [TestMethod]
    public async Task add_child_to_many_relationship_with_ID()
    {
        var make = new CarMake { Name = "make" }; await make.SaveAsync();

        var b1 = new CarModel { Name = "model1" }; await b1.SaveAsync();
        var b2 = new CarModel { Name = "model2" }; await b2.SaveAsync();

        await make.Models.AddAsync(b1.Id);
        await make.Models.AddAsync(b2.Id);

        var models = await make.Models
                          .ChildrenQueryable()
                          .OrderBy(b => b.Name)
                          .ToListAsync();

        Assert.AreEqual(2, models.Count);
        Assert.IsTrue(models[0].Name == "model1");
        Assert.IsTrue(models[1].Name == "model2");
    }


    [TestMethod]
    public async Task remove_child_from_many_relationship_with_ID()
    {
        var make = new CarMake { Name = "make" }; await make.SaveAsync();

        var b1 = new CarModel { Name = "model1" }; await b1.SaveAsync();
        var b2 = new CarModel { Name = "model2" }; await b2.SaveAsync();

        await make.Models.AddAsync(b1.Id);
        await make.Models.AddAsync(b2.Id);

        await make.Models.RemoveAsync(b1.Id);
        await make.Models.RemoveAsync(b2.Id);

        var count = await make.Models
                          .ChildrenQueryable()
                          .CountAsync();

        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public async Task overload_operator_for_adding_children_to_many_relationships()
    {
        var make = new CarMake { Name = "make" }; await make.SaveAsync();

        var b1 = new CarModel { Name = "model1" }; await b1.SaveAsync();
        var b2 = new CarModel { Name = "model2" }; await b2.SaveAsync();

        await make.Models.AddAsync(b1);
        await make.Models.AddAsync(b2.Id);

        var models = await make.Models
                          .ChildrenQueryable()
                          .OrderBy(b => b.Name)
                          .ToListAsync();

        Assert.AreEqual(2, models.Count);
        Assert.IsTrue(models[0].Name == "model1");
        Assert.IsTrue(models[1].Name == "model2");
    }

    [TestMethod]
    public async Task overload_operator_for_removing_children_from_many_relationships()
    {
        var make = new CarMake { Name = "make" }; await make.SaveAsync();

        var b1 = new CarModel { Name = "model1" }; await b1.SaveAsync();
        var b2 = new CarModel { Name = "model2" }; await b2.SaveAsync();

        await make.Models.AddAsync(b1);
        await make.Models.AddAsync(b2.Id);

        await make.Models.RemoveAsync(b1);
        await make.Models.RemoveAsync(b2.Id);

        var count = await make.Models
                          .ChildrenQueryable()
                          .CountAsync();

        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public async Task many_to_many_remove_multiple()
    {
        var a1 = new CarMake { Name = "make one" };
        var a2 = new CarMake { Name = "make two" };

        var b1 = new CarModel { Name = "model one" };
        var b2 = new CarModel { Name = "model two" };

        await new[] { a1, a2 }.SaveAsync();
        await new[] { b1, b2 }.SaveAsync();

        await a1.Models.AddAsync(new[] { b1, b2 });
        await a2.Models.AddAsync(new[] { b1, b2 });

        await a1.Models.RemoveAsync(new[] { b1, b2 });

        var a2models = await a2.Models.ChildrenQueryable().OrderBy(b => b.Name).ToListAsync();

        Assert.AreEqual(2, a2models.Count);
        Assert.AreEqual(b1.Name, a2models[0].Name);
        Assert.AreEqual(b2.Name, a2models.Last().Name);
        Assert.AreEqual(0, await a1.Models.ChildrenCountAsync());
    }
}
