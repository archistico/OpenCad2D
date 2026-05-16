using OpenCad2D.Core.Commands;
using OpenCad2D.Core.Documents;
using OpenCad2D.Core.Entities;
using OpenCad2D.Core.Identifiers;
using OpenCad2D.Geometry;
using OpenCad2D.Geometry.Coordinates;
using OpenCad2D.Geometry.Primitives;
using OpenCad2D.Interaction.Selection;
using OpenCad2D.Interaction.Snapping;
using OpenCad2D.Tools.Grips;
using OpenCad2D.Tools.Selection;
using OpenCad2D.Core.Layers;
using OpenCad2D.Core.Styling;

namespace OpenCad2D.Tools.Common;

/// <summary>
/// Aggregates the main CAD runtime services used by the application.
/// </summary>
public sealed class CadWorkspace
{
    private int _savedGeneration;
    private bool _hasExternalUnsavedChange;

    public CadWorkspace(
        CadDocument? document = null,
        CommandHistory? commandHistory = null,
        SelectionSet? selectionSet = null,
        SnapService? snapService = null,
        SelectionService? selectionService = null,
        ToolRegistry? toolRegistry = null,
        GridSettings? gridSettings = null,
        LayerId? currentLayerId = null,
        SnapKind enabledSnaps = SnapKind.None,
        double snapTolerance = 0,
        double selectionTolerance = 5,
        double selectionDragThreshold = 1,
        AngleConstraintSettings? angleConstraintSettings = null,
        ToolId initialToolId = ToolId.Selection,
        CoordinateSystem2D? currentUcs = null,
        GeometryTolerance? geometryTolerance = null)
    {
        Document = document ?? new CadDocument();
        CommandHistory = commandHistory ?? new CommandHistory();
        SelectionSet = selectionSet ?? new SelectionSet();
        SnapService = snapService ?? new SnapService();
        SelectionService = selectionService ?? new SelectionService();
        ToolRegistry = toolRegistry ?? new ToolRegistry();
        GridSettings = gridSettings ?? new GridSettings();
        GripProviders = new GripProviderRegistry();

        Context = new ToolContext(
            Document,
            CommandHistory,
            SnapService,
            selectionSet: SelectionSet,
            selectionService: SelectionService,
            gridSettings: GridSettings,
            currentLayerId: currentLayerId ?? LayerId.Default,
            enabledSnaps: enabledSnaps,
            snapTolerance: snapTolerance,
            selectionTolerance: selectionTolerance,
            selectionDragThreshold: selectionDragThreshold,
            angleConstraintSettings: angleConstraintSettings,
            currentUcs: currentUcs,
            geometryTolerance: geometryTolerance);

        ToolController = new ToolController(
            Context,
            ToolRegistry.Create(initialToolId));

        ActionController = new CadActionController(
            Context,
            ToolController);

        MarkSaved();
    }

    public CadDocument Document { get; private set; }

    public CommandHistory CommandHistory { get; }

    public SelectionSet SelectionSet { get; }

    public SnapService SnapService { get; }

    public SelectionService SelectionService { get; }

    public ToolRegistry ToolRegistry { get; }

    public GridSettings GridSettings { get; private set; }

    public GripProviderRegistry GripProviders { get; }

    public ToolContext Context { get; }

    public ToolController ToolController { get; }

    public CadActionController ActionController { get; }

    public bool IsDirty => _hasExternalUnsavedChange || CommandHistory.CurrentGeneration != _savedGeneration;

    public LayerId CurrentLayerId
    {
        get => Context.CurrentLayerId;
        set => Context.CurrentLayerId = value;
    }

    public CoordinateSystem2D CurrentUcs
    {
        get => Context.CurrentUcs;
        set => Context.CurrentUcs = value;
    }

    public GeometryTolerance GeometryTolerance
    {
        get => Context.GeometryTolerance;
        set => Context.GeometryTolerance = value;
    }

    public AngleConstraintSettings AngleConstraintSettings
    {
        get => Context.AngleConstraintSettings;
        set => Context.AngleConstraintSettings = value ?? OpenCad2D.Tools.Common.AngleConstraintSettings.Off;
    }


    public void MarkSaved()
    {
        _savedGeneration = CommandHistory.CurrentGeneration;
        _hasExternalUnsavedChange = false;
    }

    public void MarkDocumentChanged()
    {
        _hasExternalUnsavedChange = true;
        CommandHistory.RegisterExternalChange();
    }

    public void LoadDocument(
        CadDocument document,
        LayerId currentLayerId)
    {
        LoadDocument(
            document,
            currentLayerId,
            markAsSaved: true);
    }

    public void LoadDocument(
        CadDocument document,
        LayerId currentLayerId,
        bool markAsSaved)
    {
        ArgumentNullException.ThrowIfNull(document);

        Document = document;
        Context.Document = document;

        CommandHistory.Clear();
        SelectionSet.Clear();
        Context.CurrentBasePoint = null;

        CurrentLayerId = document.Layers.Contains(currentLayerId)
            ? currentLayerId
            : LayerId.Default;

        ToolController.SetActiveToolWithoutDeactivating(
            new SelectionTool());

        if (markAsSaved)
        {
            MarkSaved();
        }
        else
        {
            _savedGeneration = -1;
        }
    }

    public void NewDocument()
    {
        LoadDocument(
            new CadDocument(),
            LayerId.Default);
    }

    public ToolResult SetGridSettings(GridSettings gridSettings)
    {
        ArgumentNullException.ThrowIfNull(gridSettings);

        GridSettings = gridSettings;
        Context.Snapping.GridSettings = gridSettings;

        return ToolResult.Updated("Grid settings updated.");
    }

    public ToolResult SetGridVisible(bool isVisible)
    {
        return SetGridSettings(GridSettings.WithVisibility(isVisible));
    }

    public ToolResult SetActiveTool(ToolId toolId)
    {
        ICadTool tool = ToolRegistry.Create(toolId);

        return ToolController.SetActiveTool(tool);
    }

    public ToolResult SetActiveToolWithoutDeactivating(ToolId toolId)
    {
        ICadTool tool = ToolRegistry.Create(toolId);

        return ToolController.SetActiveToolWithoutDeactivating(tool);
    }

    public ToolResult SubmitPointFromCommandLine(Point2D worldPoint)
    {
        var pointer = new PointerInfo(
            worldPoint,
            CurrentUcs.WorldToUser(worldPoint));

        bool originalOrthoState = Context.IsOrthoEnabled;
        AngleConstraintSettings originalAngleConstraintSettings = Context.AngleConstraintSettings;
        SnapKind originalEnabledSnaps = Context.EnabledSnaps;

        try
        {
            // Command-line points have already been resolved explicitly by the caller.
            // Absolute and relative coordinates must remain exact. Direct-distance entry
            // calculates its own destination before submitting the final point.
            Context.IsOrthoEnabled = false;
            Context.AngleConstraintSettings = AngleConstraintSettings.Off;
            Context.EnabledSnaps = SnapKind.None;

            return ToolController.OnPointerPressed(pointer);
        }
        finally
        {
            Context.IsOrthoEnabled = originalOrthoState;
            Context.AngleConstraintSettings = originalAngleConstraintSettings;
            Context.EnabledSnaps = originalEnabledSnaps;
        }
    }


    public ToolResult EnterGripEditModeForSelection()
    {
        EntityId? entityId = SelectionSet.LastSelectedId;

        if (entityId is null)
        {
            return ToolResult.None("No selected entity for grip edit.");
        }

        return EnterGripEditMode(entityId.Value);
    }

    public ToolResult EnterGripEditMode(EntityId entityId)
    {
        if (!Document.Entities.TryGet(entityId, out CadEntity? entity) ||
            entity is null)
        {
            return ToolResult.None("Cannot enter grip edit mode because the entity was not found.");
        }

        if (!Document.IsEntitySelectable(entity))
        {
            return ToolResult.None("Cannot enter grip edit mode because the entity is not selectable.");
        }

        if (GripProviders.FindProvider(entity) is null)
        {
            return ToolResult.None("Selected entity does not support grip editing.");
        }

        var gripEditTool = new GripEditTool(
            entityId,
            GripProviders);

        ToolResult activationResult = gripEditTool.Activate(Context);

        if (gripEditTool.ShouldExit)
        {
            return activationResult;
        }

        ToolController.SetActiveToolWithoutDeactivating(gripEditTool);

        return activationResult.Changed
            ? activationResult
            : ToolResult.Started("Grip edit started.");
    }

    public ToolResult ExitGripEditMode()
    {
        return ToolController.SetActiveToolWithoutDeactivating(
            new SelectionTool());
    }


    public ToolResult ApplyLayerChanges(
        IEnumerable<Layer> layers,
        LayerId currentLayerId)
    {
        ArgumentNullException.ThrowIfNull(layers);

        List<Layer> layerList = layers.ToList();

        if (layerList.Count == 0)
        {
            return ToolResult.None("Layer manager requires at least one layer.");
        }

        Layer? currentLayer = layerList.FirstOrDefault(layer => layer.Id == currentLayerId);

        if (currentLayer is null)
        {
            return ToolResult.None("The current layer does not exist.");
        }

        if (!currentLayer.IsVisible || currentLayer.IsLocked)
        {
            return ToolResult.None("The current layer must be visible and unlocked.");
        }

        var command = new UpdateLayersAndCurrentLayerCommand(
            Document.Layers.All.ToList(),
            layerList,
            CurrentLayerId,
            currentLayerId,
            layerId => CurrentLayerId = layerId);

        CommandHistory.Execute(
            Document,
            command);

        int removedSelections = ClearSelectionOfNonSelectableEntities();

        string message = "Layers updated.";

        if (removedSelections > 0)
        {
            message += $" {removedSelections} selected entity/entities removed from selection.";
        }

        return ToolResult.Completed(message);
    }


    public ToolResult ApplyLineFormatChanges(IEnumerable<LineFormat> lineFormats)
    {
        ArgumentNullException.ThrowIfNull(lineFormats);

        List<LineFormat> lineFormatList = lineFormats.ToList();

        if (lineFormatList.Count == 0)
        {
            return ToolResult.None("Line format manager requires at least one format.");
        }

        if (!lineFormatList.Any(format => format.Id == LineFormatId.Continuous))
        {
            return ToolResult.None("The Continuous line format is required.");
        }

        var command = new UpdateLineFormatsCommand(
            Document.LineFormats.All.ToList(),
            lineFormatList);

        CommandHistory.Execute(
            Document,
            command);

        EnsureCurrentLayerIsUsable();
        ClearSelectionOfNonSelectableEntities();

        return ToolResult.Completed("Line formats updated.");
    }


    public ToolResult ApplyTextFormatChanges(IEnumerable<TextFormat> textFormats)
    {
        ArgumentNullException.ThrowIfNull(textFormats);

        List<TextFormat> textFormatList = textFormats.ToList();

        if (textFormatList.Count == 0)
        {
            return ToolResult.None("Text format manager requires at least one format.");
        }

        if (!textFormatList.Any(format => format.Id == TextFormatId.Standard))
        {
            return ToolResult.None("The Standard text format is required.");
        }

        var command = new UpdateTextFormatsCommand(
            Document.TextFormats.All.ToList(),
            textFormatList);

        CommandHistory.Execute(
            Document,
            command);

        ClearSelectionOfNonSelectableEntities();

        return ToolResult.Completed("Text formats updated.");
    }


    public ToolResult AssignSelectedEntitiesToCurrentLayer()
    {
        if (SelectionSet.IsEmpty)
        {
            return ToolResult.None("No selected entities to assign.");
        }

        EnsureCurrentLayerIsUsable();

        Layer currentLayer = Document.Layers.GetRequired(CurrentLayerId);

        if (!currentLayer.IsVisible || currentLayer.IsLocked)
        {
            return ToolResult.None("The current layer must be visible and unlocked.");
        }

        List<CadEntity> selectedEntities = SelectionSet.SelectedIds
            .Where(Document.Entities.Contains)
            .Select(Document.Entities.GetRequired)
            .Where(Document.IsEntitySelectable)
            .ToList();

        if (selectedEntities.Count == 0)
        {
            ClearSelectionOfNonSelectableEntities();

            return ToolResult.None("No selectable entities to assign.");
        }

        List<CadEntity> entitiesToReplace = selectedEntities
            .Where(entity => entity.LayerId != CurrentLayerId)
            .Select(entity => entity.WithLayer(CurrentLayerId))
            .ToList();

        if (entitiesToReplace.Count == 0)
        {
            return ToolResult.None($"Selected entities are already on layer '{currentLayer.Name}'.");
        }

        var command = new ReplaceEntitiesCommand(entitiesToReplace);

        CommandHistory.Execute(
            Document,
            command);

        SelectionSet.ReplaceWith(selectedEntities.Select(entity => entity.Id));

        return ToolResult.Completed(
            $"Assigned {entitiesToReplace.Count} selected entity/entities to layer '{currentLayer.Name}'.");
    }


    public void EnsureCurrentLayerIsUsable()
    {
        if (!Document.Layers.Contains(CurrentLayerId))
        {
            CurrentLayerId = LayerId.Default;
        }

        Layer currentLayer = Document.Layers.GetRequired(CurrentLayerId);

        if (currentLayer.IsVisible && !currentLayer.IsLocked)
        {
            return;
        }

        Layer? firstUsableLayer = Document.Layers.All
            .OrderBy(layer => layer.Name)
            .FirstOrDefault(layer => layer.IsVisible && !layer.IsLocked);

        CurrentLayerId = firstUsableLayer?.Id ?? LayerId.Default;
    }

    public ToolResult SetCurrentLayerVisibility(bool isVisible)
    {
        Layer currentLayer = Document.Layers.GetRequired(CurrentLayerId);

        if (!isVisible)
        {
            return ToolResult.None("The current layer must remain visible.");
        }

        if (currentLayer.IsVisible)
        {
            return ToolResult.None($"Layer '{currentLayer.Name}' is already visible.");
        }

        List<Layer> updatedLayers = Document.Layers.All
            .Select(layer => layer.Id == CurrentLayerId
                ? layer.WithVisibility(true)
                : layer)
            .ToList();

        var command = new UpdateLayersAndCurrentLayerCommand(
            Document.Layers.All.ToList(),
            updatedLayers,
            CurrentLayerId,
            CurrentLayerId,
            layerId => CurrentLayerId = layerId);

        CommandHistory.Execute(
            Document,
            command);

        return ToolResult.Completed($"Layer '{currentLayer.Name}' visible.");
    }

    public ToolResult SetCurrentLayerLocked(bool isLocked)
    {
        Layer currentLayer = Document.Layers.GetRequired(CurrentLayerId);

        if (currentLayer.IsLocked == isLocked)
        {
            string alreadyMessage = isLocked
                ? $"Layer '{currentLayer.Name}' is already locked."
                : $"Layer '{currentLayer.Name}' is already unlocked.";

            return ToolResult.None(alreadyMessage);
        }

        List<Layer> updatedLayers = Document.Layers.All
            .Select(layer => layer.Id == CurrentLayerId
                ? layer.WithLocked(isLocked)
                : layer)
            .ToList();

        LayerId nextCurrentLayerId = ResolveUsableCurrentLayerId(
            updatedLayers,
            CurrentLayerId);

        var command = new UpdateLayersAndCurrentLayerCommand(
            Document.Layers.All.ToList(),
            updatedLayers,
            CurrentLayerId,
            nextCurrentLayerId,
            layerId => CurrentLayerId = layerId);

        CommandHistory.Execute(
            Document,
            command);

        int removedSelections = ClearSelectionOfNonSelectableEntities();

        string message = isLocked
            ? "Current layer locked."
            : "Current layer unlocked.";

        if (removedSelections > 0)
        {
            message += $" {removedSelections} selected entity/entities removed from selection.";
        }

        return ToolResult.Completed(message);
    }

    private static LayerId ResolveUsableCurrentLayerId(
        IReadOnlyList<Layer> layers,
        LayerId preferredLayerId)
    {
        Layer? preferredLayer = layers.FirstOrDefault(layer => layer.Id == preferredLayerId);

        if (preferredLayer is not null &&
            preferredLayer.IsVisible &&
            !preferredLayer.IsLocked)
        {
            return preferredLayer.Id;
        }

        Layer? firstUsableLayer = layers
            .OrderBy(layer => layer.Name)
            .FirstOrDefault(layer => layer.IsVisible && !layer.IsLocked);

        return firstUsableLayer?.Id ?? preferredLayerId;
    }

    public int ClearSelectionOfNonSelectableEntities()
    {
        int removed = 0;

        foreach (EntityId entityId in SelectionSet.SelectedIds.ToList())
        {
            if (!Document.Entities.Contains(entityId))
            {
                SelectionSet.Deselect(entityId);
                removed++;
                continue;
            }

            CadEntity entity = Document.Entities.GetRequired(entityId);

            if (Document.IsEntitySelectable(entity))
            {
                continue;
            }

            SelectionSet.Deselect(entityId);
            removed++;
        }

        return removed;
    }

    public ToolResult Escape()
    {
        if (ToolController.ActiveTool is GripEditTool gripEditTool)
        {
            ToolResult gripCancelResult = gripEditTool.Cancel(Context);

            if (gripEditTool.ShouldExit)
            {
                ExitGripEditMode();

                return ToolResult.Cancelled("Grip edit exited.");
            }

            return gripCancelResult;
        }

        if (ToolController.ActiveTool is SelectionTool selectionTool)
        {
            if (!selectionTool.HasWindowPreview && SelectionSet.IsEmpty)
            {
                return ToolResult.None();
            }

            return ActionController.CancelActiveTool();
        }

        string activeToolName = ToolController.ActiveToolName;

        ToolResult cancelResult = ActionController.CancelActiveTool();

        ToolController.SetActiveToolWithoutDeactivating(
            new SelectionTool());

        return cancelResult.Changed
            ? ToolResult.Cancelled($"{activeToolName} command cancelled. Selection tool active.")
            : ToolResult.Cancelled("Selection tool active.");
    }
}
