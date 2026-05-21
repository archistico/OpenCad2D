using System.Linq;
using OpenCad2D.App.ViewModels.DimensionStyles;
using OpenCad2D.Core.Dimensions;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry.Primitives;

namespace OpenCad2D.App.Tests;

public sealed class DimensionStyleManagerWindowViewModelTests
{
    [Fact]
    public void Constructor_ShouldExposeDocumentDimensionStylesAndCurrentStyle()
    {
        var document = new CadDocument();

        var viewModel = new DimensionStyleManagerWindowViewModel(document);

        Assert.Equal(document.DimensionStyles.Count, viewModel.Styles.Count);
        Assert.Equal(DimensionStyleId.Standard, viewModel.CurrentStyle!.Id);
        Assert.Contains(viewModel.Styles, style => style.Id == DimensionStyleId.Standard);
    }

    [Fact]
    public void AddStyle_ShouldCreateEditableCopyFromSelectedStyle()
    {
        var document = new CadDocument();
        var viewModel = new DimensionStyleManagerWindowViewModel(document);

        viewModel.AddStyle();

        EditableDimensionStyleViewModel added = viewModel.SelectedStyle!;

        Assert.False(added.IsBuiltIn);
        Assert.Equal("New dimension style", added.Name);
        Assert.Equal("4", added.ArrowSizeText);
        Assert.Equal(DimensionArrowSymbol.ClosedArrow, added.ArrowSymbol);
        Assert.Equal(DimensionTextRotationMode.Readable, added.TextRotationMode);
        Assert.Equal(DimensionTextFitMode.OutsideWhenNeeded, added.TextFitMode);
        Assert.Equal(DimensionTerminatorFitMode.OutsideWhenNeeded, added.TerminatorFitMode);
    }

    [Fact]
    public void DeleteSelectedStyle_WhenBuiltIn_ShouldReject()
    {
        var document = new CadDocument();
        var viewModel = new DimensionStyleManagerWindowViewModel(document);
        viewModel.SelectedStyle = viewModel.Styles.Single(style => style.Id == DimensionStyleId.Standard);

        int before = viewModel.Styles.Count;

        viewModel.DeleteSelectedStyle();

        Assert.Equal(before, viewModel.Styles.Count);
        Assert.True(viewModel.HasValidationMessage);
    }

    [Fact]
    public void DeleteSelectedStyle_WhenUsedByDimension_ShouldReject()
    {
        var document = new CadDocument();
        var customId = new DimensionStyleId("Custom");
        document.DimensionStyles.ReplaceAll(document.DimensionStyles.All.Append(CreateStyle(customId, "Custom")));
        document.AddEntity(new LinearDimensionEntity(
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(0, 5),
            DimensionOrientation.Horizontal,
            customId));

        var viewModel = new DimensionStyleManagerWindowViewModel(document);
        viewModel.SelectedStyle = viewModel.Styles.Single(style => style.Id == customId);

        viewModel.DeleteSelectedStyle();

        Assert.Contains(viewModel.Styles, style => style.Id == customId);
        Assert.True(viewModel.HasValidationMessage);
    }

    [Fact]
    public void SetSelectedAsCurrent_ShouldUpdateCurrentStyle()
    {
        var document = new CadDocument();
        var viewModel = new DimensionStyleManagerWindowViewModel(document);
        viewModel.AddStyle();
        EditableDimensionStyleViewModel added = viewModel.SelectedStyle!;

        viewModel.SetSelectedAsCurrent();

        Assert.Same(added, viewModel.CurrentStyle);
    }

    [Fact]
    public void TryBuildResult_ShouldReturnEditedStylesAndCurrentStyle()
    {
        var document = new CadDocument();
        var viewModel = new DimensionStyleManagerWindowViewModel(document);
        EditableDimensionStyleViewModel standard = viewModel.Styles.Single(style => style.Id == DimensionStyleId.Standard);

        standard.Name = "Standard quoted";
        standard.Suffix = " m";
        standard.DecimalPlacesText = "3";
        standard.ArrowSizeText = "2.5";
        standard.ArrowSymbol = DimensionArrowSymbol.OpenArrow;
        standard.TextRotationMode = DimensionTextRotationMode.Horizontal;
        standard.TextFitMode = DimensionTextFitMode.AlwaysOutside;
        standard.TerminatorFitMode = DimensionTerminatorFitMode.AlwaysOutside;

        bool success = viewModel.TryBuildResult(out DimensionStyleManagerResult result);

        DimensionStyle edited = result.DimensionStyles.Single(style => style.Id == DimensionStyleId.Standard);

        Assert.True(success);
        Assert.Equal(DimensionStyleId.Standard, result.CurrentDimensionStyleId);
        Assert.Equal("Standard quoted", edited.Name);
        Assert.Equal(" m", edited.Suffix);
        Assert.Equal(3, edited.DecimalPlaces);
        Assert.Equal(2.5, edited.ArrowSize);
        Assert.Equal(DimensionArrowSymbol.OpenArrow, edited.ArrowSymbol);
        Assert.Equal(DimensionTextRotationMode.Horizontal, edited.TextRotationMode);
        Assert.Equal(DimensionTextFitMode.AlwaysOutside, edited.TextFitMode);
        Assert.Equal(DimensionTerminatorFitMode.AlwaysOutside, edited.TerminatorFitMode);
    }

    [Fact]
    public void TryBuildResult_WithDuplicateNames_ShouldReject()
    {
        var document = new CadDocument();
        var viewModel = new DimensionStyleManagerWindowViewModel(document);
        viewModel.AddStyle();

        viewModel.Styles.Single(style => style.Id == DimensionStyleId.Standard).Name = "Same";
        viewModel.SelectedStyle!.Name = "same";

        bool success = viewModel.TryBuildResult(out _);

        Assert.False(success);
        Assert.True(viewModel.HasValidationMessage);
    }


    [Fact]
    public void TryBuildResult_WithNegativeTextOffset_ShouldAcceptValue()
    {
        var document = new CadDocument();
        var viewModel = new DimensionStyleManagerWindowViewModel(document);
        EditableDimensionStyleViewModel standard = viewModel.Styles.Single(style => style.Id == DimensionStyleId.Standard);
        standard.TextOffsetText = "-2.5";

        bool success = viewModel.TryBuildResult(out DimensionStyleManagerResult result);

        DimensionStyle edited = result.DimensionStyles.Single(style => style.Id == DimensionStyleId.Standard);

        Assert.True(success);
        Assert.Equal(-2.5, edited.TextOffset);
    }

    [Fact]
    public void TryBuildResult_WithInvalidArrowSize_ShouldReject()
    {
        var document = new CadDocument();
        var viewModel = new DimensionStyleManagerWindowViewModel(document);
        viewModel.Styles.Single(style => style.Id == DimensionStyleId.Standard).ArrowSizeText = "0";

        bool success = viewModel.TryBuildResult(out _);

        Assert.False(success);
        Assert.True(viewModel.HasValidationMessage);
    }

    private static DimensionStyle CreateStyle(
        DimensionStyleId id,
        string name)
    {
        return new DimensionStyle(
            id,
            name,
            TextFormatId.Annotation,
            arrowSize: 4.0,
            textOffset: 2.0,
            extensionLineOffset: 1.5,
            extensionLineOvershoot: 2.0,
            decimalPlaces: 2);
    }
}
