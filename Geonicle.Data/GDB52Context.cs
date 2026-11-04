#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Data.Models;

public partial class GDB52Context : DbContext
{
    private readonly string _сonnString;

    public GDB52Context()
    { }

    public GDB52Context(DbContextOptions<GDB52Context> options)
        : base(options)
    {
    }

    public GDB52Context(string connString)
    {
        _сonnString = connString;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder
                .UseSqlServer(_сonnString)
                // В ошибке чтения значения называет сущность и свойство (вместо «Data is Null» без подробностей).
                .EnableDetailedErrors();
    }

    public virtual DbSet<Assay> Assays { get; set; }

    public virtual DbSet<AssayGroup> AssayGroups { get; set; }

    public virtual DbSet<AssayVar> AssayVars { get; set; }

    public virtual DbSet<AssayVarsCr> AssayVarsCrs { get; set; }

    public virtual DbSet<Average> Averages { get; set; }

    public virtual DbSet<Block> Blocks { get; set; }

    public virtual DbSet<BlockDetail> BlockDetails { get; set; }

    public virtual DbSet<BlockUnit> BlockUnits { get; set; }

    public virtual DbSet<Borehole> Boreholes { get; set; }

    public virtual DbSet<ChangedRecord> ChangedRecords { get; set; }

    public virtual DbSet<Coeff> Coeffs { get; set; }

    public virtual DbSet<Composite> Composites { get; set; }

    public virtual DbSet<CondDef> CondDefs { get; set; }

    public virtual DbSet<Condition> Conditions { get; set; }

    public virtual DbSet<Dictionary> Dictionaries { get; set; }

    public virtual DbSet<Dimension> Dimensions { get; set; }

    public virtual DbSet<Drawing> Drawings { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<GroundWater> GroundWaters { get; set; }

    public virtual DbSet<HoleGroup> HoleGroups { get; set; }

    public virtual DbSet<HoleSeam> HoleSeams { get; set; }

    public virtual DbSet<HoleSeamAssay> HoleSeamAssays { get; set; }

    public virtual DbSet<Hssamp> Hssamps { get; set; }

    public virtual DbSet<InclinedHole> InclinedHoles { get; set; }

    public virtual DbSet<Line> Lines { get; set; }

    public virtual DbSet<LineHole> LineHoles { get; set; }

    public virtual DbSet<LineHolesDistinct> LineHolesDistincts { get; set; }

    public virtual DbSet<LithRe> LithRes { get; set; }

    public virtual DbSet<Lithology> Lithologies { get; set; }

    public virtual DbSet<Num> Nums { get; set; }

    public virtual DbSet<ObjectsCoord> ObjectsCoords { get; set; }

    public virtual DbSet<OrderedSeam> OrderedSeams { get; set; }

    public virtual DbSet<Part> Parts { get; set; }

    public virtual DbSet<PlanGeom> PlanGeoms { get; set; }

    public virtual DbSet<Point> Points { get; set; }

    public virtual DbSet<ProfileGeom> ProfileGeoms { get; set; }

    public virtual DbSet<Query> Queries { get; set; }

    public virtual DbSet<QueryParam> QueryParams { get; set; }

    public virtual DbSet<RefTable> RefTables { get; set; }

    public virtual DbSet<ResParam> ResParams { get; set; }

    public virtual DbSet<Rock> Rocks { get; set; }

    public virtual DbSet<RockGroup> RockGroups { get; set; }

    public virtual DbSet<Sample> Samples { get; set; }

    public virtual DbSet<SampleDef> SampleDefs { get; set; }

    public virtual DbSet<Seam> Seams { get; set; }

    public virtual DbSet<SeamCond> SeamConds { get; set; }

    public virtual DbSet<SeamDef> SeamDefs { get; set; }

    public virtual DbSet<SeamPoint> SeamPoints { get; set; }

    public virtual DbSet<Setting> Settings { get; set; }

    public virtual DbSet<Survey> Surveys { get; set; }

    public virtual DbSet<SurveyCoord> SurveyCoords { get; set; }

    public virtual DbSet<TechAssay> TechAssays { get; set; }

    public virtual DbSet<Variant> Variants { get; set; }

    public virtual DbSet<Version> Versions { get; set; }

    public virtual DbSet<VerticalHole> VerticalHoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Отображение каждой сущности описано в Models/Configurations/<Сущность>Configuration.cs.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GDB52Context).Assembly);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}