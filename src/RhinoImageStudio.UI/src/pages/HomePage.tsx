import { useState, useEffect } from 'react';
import { api } from '@/lib/api';
import { Project } from '@/lib/types';
import { ProjectCard } from '@/components/Sessions/SessionCard';
import { GenerationGallery } from '@/components/Home/GenerationGallery';
import { CreateProjectModal } from '@/components/Sessions/CreateSessionModal';
import { Button } from '@/components/Common/Button';
import { ConfirmDialog } from '@/components/Common/ConfirmDialog';
import { useToast } from '@/components/Common/ToastProvider';
import { ThemeSwitch } from '@/components/Common/ThemeSwitch';
import { Plus, Search, FolderOpen, Image } from 'lucide-react';
import { Input } from '@/components/Common/Input';

type TabType = 'projects' | 'generations';

export function HomePage() {
  const { toast } = useToast();
  const [activeTab, setActiveTab] = useState<TabType>('projects');
  const [projects, setProjects] = useState<Project[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [projectToDeleteId, setProjectToDeleteId] = useState<string | null>(null);

  const getErrorMessage = (error: unknown, fallback: string): string => {
    if (error instanceof Error && error.message) return error.message;
    return fallback;
  };

  const loadData = async () => {
    setIsLoading(true);
    try {
      const projectsData = await api.projects.list();
      setProjects(projectsData);
    } catch (error) {
      console.error(error);
      toast({
        title: 'Failed to load dashboard',
        description: getErrorMessage(error, 'Unknown error while loading projects'),
        variant: 'error',
      });
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handlePin = async (id: string, current: boolean) => {
    try {
      await api.projects.togglePin(id, !current);
      loadData();
    } catch (error) {
      console.error(error);
      toast({
        title: 'Pin update failed',
        description: getErrorMessage(error, 'Could not update pin status'),
        variant: 'error',
      });
    }
  };

  const handleRename = async (id: string, newName: string) => {
    try {
      await api.projects.update(id, { name: newName });
      loadData();
    } catch (error) {
      console.error(error);
      toast({
        title: 'Rename failed',
        description: getErrorMessage(error, 'Could not rename project'),
        variant: 'error',
      });
    }
  };

  const handleDelete = async (id: string) => {
    setProjectToDeleteId(id);
  };

  const confirmDeleteProject = async () => {
    if (!projectToDeleteId) return;
    try {
      await api.projects.delete(projectToDeleteId);
      loadData();
      setProjectToDeleteId(null);
    } catch (error) {
      console.error(error);
      toast({
        title: 'Delete failed',
        description: getErrorMessage(error, 'Could not delete project'),
        variant: 'error',
      });
    }
  };

  const filteredProjects = projects
    .filter(p => p.name.toLowerCase().includes(search.toLowerCase()))
    .sort((a, b) => {
      if (a.isPinned && !b.isPinned) return -1;
      if (!a.isPinned && b.isPinned) return 1;
      return new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime();
    });

  return (
    <div className="h-screen overflow-y-auto bg-background p-8">
      <div className="max-w-7xl mx-auto space-y-8">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-4xl font-bold tracking-tight" style={{ color: '#3D5A64' }}>Rhino Image Studio</h1>
            <p className="text-secondary mt-2">AI-powered rendering and visualization</p>
          </div>
          <div className="flex items-center gap-3">
            <ThemeSwitch />
            <Button size="lg" onClick={() => setIsCreateModalOpen(true)}>
              <Plus className="mr-2 h-4 w-4" />
              New Project
            </Button>
          </div>
        </div>

        {/* Tab Navigation */}
        <div className="flex items-center gap-4 border-b border-border">
          <button
            onClick={() => setActiveTab('projects')}
            className={`flex items-center gap-2 px-4 py-3 border-b-2 transition-colors ${
              activeTab === 'projects'
                ? 'border-primary text-primary font-medium'
                : 'border-transparent text-secondary hover:text-primary'
            }`}
          >
            <FolderOpen className="h-4 w-4" />
            My Projects
          </button>
          <button
            onClick={() => setActiveTab('generations')}
            className={`flex items-center gap-2 px-4 py-3 border-b-2 transition-colors ${
              activeTab === 'generations'
                ? 'border-primary text-primary font-medium'
                : 'border-transparent text-secondary hover:text-primary'
            }`}
          >
            <Image className="h-4 w-4" />
            Generations
          </button>
        </div>

        {activeTab === 'projects' && (
          <>
            <div className="flex items-center space-x-4">
              <div className="relative flex-1 max-w-sm">
                <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-secondary" />
                <Input
                  placeholder="Search projects..."
                  className="pl-9"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </div>
            </div>

            {isLoading ? (
              <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                {Array.from({ length: 8 }).map((_, i) => (
                  <div key={i} className="overflow-hidden rounded-xl border border-border bg-card">
                    <div className="aspect-video animate-pulse bg-card-hover" />
                    <div className="space-y-2 p-4">
                      <div className="h-4 w-2/3 animate-pulse rounded bg-card-hover" />
                      <div className="h-3 w-1/3 animate-pulse rounded bg-card-hover" />
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                {filteredProjects.map((project) => (
                  <ProjectCard
                    key={project.id}
                    project={project}
                    onPin={handlePin}
                    onRename={handleRename}
                    onDelete={handleDelete}
                  />
                ))}
                {filteredProjects.length === 0 && (
                  <div className="col-span-full flex h-64 flex-col items-center justify-center rounded-lg border border-dashed border-border text-center">
                    <p className="text-secondary">No projects found</p>
                    <Button variant="link" onClick={() => setIsCreateModalOpen(true)}>
                      Create your first project
                    </Button>
                  </div>
                )}
              </div>
            )}
          </>
        )}

        {activeTab === 'generations' && <GenerationGallery isActive={activeTab === 'generations'} />}
      </div>

      <CreateProjectModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onCreated={loadData}
      />

      <ConfirmDialog
        isOpen={!!projectToDeleteId}
        title="Delete Project"
        description="Are you sure you want to delete this project? This action cannot be undone."
        confirmLabel="Delete"
        cancelLabel="Cancel"
        destructive
        onConfirm={confirmDeleteProject}
        onCancel={() => setProjectToDeleteId(null)}
      />
    </div>
  );
}
