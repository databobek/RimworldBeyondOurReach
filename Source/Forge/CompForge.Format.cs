using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BeyondOurReach;

public partial class CompForge
{
    public const int FormatTicksPerCell = GenDate.TicksPerHour;

    private List<int> formatQueue = new();
    private int formatTicks;
    private int formatDone;

    public bool IsFormatting
    {
        get
        {
            PruneFormatQueue();
            return formatQueue.Count > 0;
        }
    }

    public int FormatColumn => IsFormatting ? formatQueue[0] : -1;

    public int FormatDone => formatDone;

    public int FormatTotal => formatDone + FilledInColumn(FormatColumn);

    public float FormatCellProgress => (float)formatTicks / FormatTicksPerCell;

    public int FilledInColumn(int col)
    {
        EnsureSize();
        int width = Props.gridWidth;
        int n = 0;
        for (int i = col; col >= 0 && i < slots.Count; i += width)
        {
            if (slots[i] != null)
            {
                n++;
            }
        }
        return n;
    }

    private void PruneFormatQueue()
    {
        while (formatQueue.Count > 0 && FilledInColumn(formatQueue[0]) == 0)
        {
            formatQueue.RemoveAt(0);
            formatTicks = 0;
            formatDone = 0;
        }
    }

    public void QueueFormat(int col)
    {
        if (FilledInColumn(col) > 0 && !formatQueue.Contains(col))
        {
            formatQueue.Add(col);
        }
    }

    // Advances formatting by one tick; each full hour wipes the topmost filled cell of the active column.
    public void FormatTick()
    {
        if (!IsFormatting)
        {
            return;
        }
        formatTicks++;
        if (formatTicks < FormatTicksPerCell)
        {
            return;
        }
        formatTicks = 0;
        int width = Props.gridWidth;
        for (int i = formatQueue[0]; i < slots.Count; i += width)
        {
            if (slots[i] != null)
            {
                slots[i] = null;
                slotStuff[i] = null;
                queued[i] = 0;
                modes[i] = 0;
                progress[i] = 0f;
                formatDone++;
                break;
            }
        }
        PruneFormatQueue();
    }
}
