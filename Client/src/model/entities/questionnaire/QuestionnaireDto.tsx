// The gym's single health questionnaire.
export interface QuestionnaireQuestionDto {
  id: number;
  orderNumber: number;
  title: string;
  // The question itself, as html from the rich text editor.
  details: string;
  // Optional picture as a data url - a body chart with numbered areas, say.
  image: string;
  answerPlaceholder: string;
}

export class QuestionnaireQuestionDto {
  id: number = 0;
  orderNumber: number = 0;
  title: string = "";
  details: string = "";
  image: string = "";
  answerPlaceholder: string = "";
}

export interface QuestionnaireDto {
  id: number;
  name: string;
  questions: QuestionnaireQuestionDto[];
}

export class QuestionnaireDto {
  id: number = 0;
  name: string = "";
  questions: QuestionnaireQuestionDto[] = [];
}

export interface QuestionnaireAnswerDto {
  questionnaireQuestionId: number;
  answer: string;
}
