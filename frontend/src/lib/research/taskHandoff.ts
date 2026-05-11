import type { ResearchTaskQueueItem } from './types';

export interface ResearchTaskHandoffPayload {
  schemaVersion: '1.0.0';
  generatedAtUtc: string;
  filters: {
    focusedCompoundSlug?: string;
    priority?: string;
    requester?: string;
    category?: string;
  };
  tasks: Array<{
    taskId: string;
    compoundName: string;
    aliases: string[];
    categories: string[];
    classification: string;
    priority: string;
    requestIds: string[];
    requesterIds: string[];
    rationales: string[];
    notes: string[];
    suggestedResearchDirectives: string[];
    targetEvidencePath: string;
    requiredSchema: string;
  }>;
}

export function buildResearchTaskHandoffPayload(
  tasks: ResearchTaskQueueItem[],
  generatedAtUtc: string,
  filters: ResearchTaskHandoffPayload['filters']
): ResearchTaskHandoffPayload {
  return {
    schemaVersion: '1.0.0',
    generatedAtUtc,
    filters,
    tasks: tasks.map((task) => ({
      taskId: task.taskId,
      compoundName: task.compoundName,
      aliases: task.aliases,
      categories: task.categories ?? [],
      classification: task.classification,
      priority: task.priority,
      requestIds: task.requestIds,
      requesterIds: task.requesterIds,
      rationales: task.rationales,
      notes: task.notes,
      suggestedResearchDirectives: task.suggestedResearchDirectives,
      targetEvidencePath: task.targetEvidencePath,
      requiredSchema: task.requiredSchema,
    })),
  };
}

export function buildResearchTaskExportHref(payload: ResearchTaskHandoffPayload): string {
  return `data:application/json;charset=utf-8,${encodeURIComponent(JSON.stringify(payload, null, 2))}`;
}