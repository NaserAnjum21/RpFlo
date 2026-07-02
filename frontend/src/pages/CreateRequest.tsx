import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Plus, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { procurementApi } from '@/api/procurement';
import type { CreateProcurementRequest } from '@/types';

const departments = ['Engineering', 'Marketing', 'Sales', 'Operations', 'HumanResources', 'Finance'];
const urgencies = ['Low', 'Medium', 'High', 'Critical'];

interface LineItemForm {
  name: string;
  quantity: string;
  unitPrice: string;
}

export function CreateRequest() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [department, setDepartment] = useState('');
  const [urgency, setUrgency] = useState('');
  const [lineItems, setLineItems] = useState<LineItemForm[]>([
    { name: '', quantity: '', unitPrice: '' },
  ]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const createMutation = useMutation({
    mutationFn: (data: CreateProcurementRequest) => procurementApi.create(data),
    onSuccess: (response) => {
      queryClient.invalidateQueries({ queryKey: ['procurement'] });
      navigate(`/requests/${response.id}`);
    },
    onError: (error: any) => {
      if (error.response?.data?.errors) {
        setErrors(error.response.data.errors);
      }
    },
  });

  const addLineItem = () => {
    setLineItems(prev => [...prev, { name: '', quantity: '', unitPrice: '' }]);
  };

  const removeLineItem = (index: number) => {
    setLineItems(prev => prev.filter((_, i) => i !== index));
  };

  const updateLineItem = (index: number, field: keyof LineItemForm, value: string) => {
    setLineItems(prev => prev.map((item, i) => (i === index ? { ...item, [field]: value } : item)));
  };

  const totalAmount = lineItems.reduce((sum, item) => {
    const qty = parseInt(item.quantity) || 0;
    const price = parseFloat(item.unitPrice) || 0;
    return sum + qty * price;
  }, 0);

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};
    if (!title.trim()) newErrors.title = 'Title is required';
    if (!description.trim()) newErrors.description = 'Description is required';
    if (!department) newErrors.department = 'Department is required';
    if (!urgency) newErrors.urgency = 'Urgency is required';
    if (lineItems.length === 0) newErrors.lineItems = 'At least one line item is required';

    lineItems.forEach((item, i) => {
      if (!item.name.trim()) newErrors[`lineItem_${i}_name`] = 'Required';
      if (!item.quantity || parseInt(item.quantity) <= 0) newErrors[`lineItem_${i}_quantity`] = 'Must be > 0';
      if (!item.unitPrice || parseFloat(item.unitPrice) <= 0) newErrors[`lineItem_${i}_unitPrice`] = 'Must be > 0';
    });

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    createMutation.mutate({
      title: title.trim(),
      description: description.trim(),
      department,
      urgency,
      lineItems: lineItems.map(item => ({
        name: item.name.trim(),
        quantity: parseInt(item.quantity),
        unitPrice: parseFloat(item.unitPrice),
      })),
    });
  };

  return (
    <div className="space-y-6 max-w-3xl mx-auto">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" onClick={() => navigate(-1)}>
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <h1 className="text-2xl font-bold">New Procurement Request</h1>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Request Details</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <Label htmlFor="title">Title</Label>
              <Input
                id="title"
                value={title}
                onChange={e => setTitle(e.target.value)}
                placeholder="e.g., Development Laptops"
                className={errors.title ? 'border-red-500' : ''}
              />
              {errors.title && <p className="text-sm text-red-500 mt-1">{errors.title}</p>}
            </div>

            <div>
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                value={description}
                onChange={e => setDescription(e.target.value)}
                placeholder="Describe what you need and why..."
                rows={3}
                className={errors.description ? 'border-red-500' : ''}
              />
              {errors.description && <p className="text-sm text-red-500 mt-1">{errors.description}</p>}
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Department</Label>
                <Select value={department} onValueChange={v => v && setDepartment(v)}>
                  <SelectTrigger className={errors.department ? 'border-red-500' : ''}>
                    <SelectValue placeholder="Select department" />
                  </SelectTrigger>
                  <SelectContent>
                    {departments.map(d => (
                      <SelectItem key={d} value={d}>{d}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {errors.department && <p className="text-sm text-red-500 mt-1">{errors.department}</p>}
              </div>

              <div>
                <Label>Urgency</Label>
                <Select value={urgency} onValueChange={v => v && setUrgency(v)}>
                  <SelectTrigger className={errors.urgency ? 'border-red-500' : ''}>
                    <SelectValue placeholder="Select urgency" />
                  </SelectTrigger>
                  <SelectContent>
                    {urgencies.map(u => (
                      <SelectItem key={u} value={u}>{u}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {errors.urgency && <p className="text-sm text-red-500 mt-1">{errors.urgency}</p>}
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle className="text-base">Line Items</CardTitle>
            <Button type="button" variant="outline" size="sm" onClick={addLineItem} className="gap-1">
              <Plus className="h-3 w-3" />
              Add Item
            </Button>
          </CardHeader>
          <CardContent className="space-y-4">
            {lineItems.map((item, index) => (
              <div key={index} className="flex gap-3 items-start">
                <div className="flex-1">
                  <Input
                    placeholder="Item name"
                    value={item.name}
                    onChange={e => updateLineItem(index, 'name', e.target.value)}
                    className={errors[`lineItem_${index}_name`] ? 'border-red-500' : ''}
                  />
                </div>
                <div className="w-24">
                  <Input
                    type="number"
                    placeholder="Qty"
                    value={item.quantity}
                    onChange={e => updateLineItem(index, 'quantity', e.target.value)}
                    min="1"
                    className={errors[`lineItem_${index}_quantity`] ? 'border-red-500' : ''}
                  />
                </div>
                <div className="w-32">
                  <Input
                    type="number"
                    placeholder="Unit Price"
                    value={item.unitPrice}
                    onChange={e => updateLineItem(index, 'unitPrice', e.target.value)}
                    min="0.01"
                    step="0.01"
                    className={errors[`lineItem_${index}_unitPrice`] ? 'border-red-500' : ''}
                  />
                </div>
                <div className="w-24 text-right pt-2 text-sm font-medium">
                  ${((parseInt(item.quantity) || 0) * (parseFloat(item.unitPrice) || 0)).toFixed(2)}
                </div>
                {lineItems.length > 1 && (
                  <Button type="button" variant="ghost" size="icon" onClick={() => removeLineItem(index)}>
                    <Trash2 className="h-4 w-4 text-muted-foreground" />
                  </Button>
                )}
              </div>
            ))}
            {errors.lineItems && <p className="text-sm text-red-500">{errors.lineItems}</p>}

            <div className="flex justify-end pt-2 border-t">
              <p className="text-lg font-bold">
                Total: ${totalAmount.toLocaleString('en-US', { minimumFractionDigits: 2 })}
              </p>
            </div>
          </CardContent>
        </Card>

        <div className="flex gap-3 justify-end">
          <Button type="button" variant="outline" onClick={() => navigate(-1)}>
            Cancel
          </Button>
          <Button type="submit" disabled={createMutation.isPending}>
            {createMutation.isPending ? 'Creating...' : 'Create Request'}
          </Button>
        </div>
      </form>
    </div>
  );
}
